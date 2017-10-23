namespace Candado.Core

open System
open System.IO
open System.Text
open System.Security.Cryptography
open Microsoft.Win32
open ROP.Toolkit
open ROP.Toolkit.Operators

[<AutoOpen>]
module private Common =

    [<Literal>]
    let CandadoField = "Software\Candado"

    [<Literal>]
    let SecretKeyField = "SecretKey"

    [<Literal>]
    let MasterPswField = "MasterPsw"

    let getRootRegistry () =
        let registry = Registry.CurrentUser.OpenSubKey(CandadoField, true)
        if isNull registry then
            failwith <| sprintf "Registry '%s' not found" CandadoField
        else registry

module RegistryHelper =

    let Init secretKey masterPsw =
        let validateArgs root =
            if String.IsNullOrEmpty(secretKey) then
                TwoTrack.fail "Secret key is required"
            elif String.IsNullOrEmpty(masterPsw) then
                TwoTrack.fail "Master password is required"
            else TwoTrack.succeed root

        let getCandadoRegistry () =
            let registry = Registry.CurrentUser.OpenSubKey(CandadoField, true)
            if isNull registry then
                Registry.CurrentUser.CreateSubKey(CandadoField)
            else registry
            
        let setSecretKeyRegistry (root: RegistryKey) =
            let value = root.GetValue(SecretKeyField) :?> string
            if not <| String.IsNullOrEmpty(value) then
                printfn "Secret key has already been set"
            else root.SetValue(SecretKeyField, secretKey)
            root   

        let setMasterPswRegistry (root: RegistryKey) =
            let value = root.GetValue(MasterPswField) :?> string
            if not <| String.IsNullOrEmpty(value) then
                printfn "Master password has already been set"
            else root.SetValue(MasterPswField, masterPsw)
            root
            
        let throwException error =
            failwith error
        
        let closeRegistry (root: RegistryKey) =
            root.Close()

        let execute =
            getCandadoRegistry
            >> validateArgs
            >> TwoTrack.map setSecretKeyRegistry
            >> TwoTrack.map setMasterPswRegistry
            >> TwoTrack.map closeRegistry
            >> TwoTrack.valueOr throwException
            
        execute()

type ICryptoService =
    abstract member Decrypt : string -> string -> string
    abstract member Encrypt : string -> string -> string

type CryptoService() =

    let createDes (key: string) =
        use md5 = new MD5CryptoServiceProvider()
        let des = new TripleDESCryptoServiceProvider()
        let size = des.BlockSize / 8
        let bytes = Encoding.Unicode.GetBytes(key)

        des.Key <- md5.ComputeHash(bytes)
        des.IV <- Array.zeroCreate (size)
        des

    interface ICryptoService with

        member __.Decrypt key text =
            use stream = new MemoryStream()
            let des = createDes key
            use cryptoStream = new CryptoStream(stream, des.CreateDecryptor(), CryptoStreamMode.Write)
            let bytes = Convert.FromBase64String(text)

            cryptoStream.Write(bytes, 0, bytes.Length)
            cryptoStream.FlushFinalBlock()

            let array = stream.ToArray()

            Encoding.Unicode.GetString(array)

        member __.Encrypt key text =
            use stream = new MemoryStream()
            let des = createDes key
            use cryptoStream = new CryptoStream(stream, des.CreateEncryptor(), CryptoStreamMode.Write)
            let bytes = Encoding.Unicode.GetBytes(text)

            cryptoStream.Write(bytes, 0, bytes.Length)
            cryptoStream.FlushFinalBlock()

            let array = stream.ToArray()

            Convert.ToBase64String(array)

type ISecretKeyProvider =
    abstract member GetSecretKey : unit -> string;

type SecretKeyProvider() =

    interface ISecretKeyProvider with

        member __.GetSecretKey () =
            let registry = getRootRegistry()
            let value = registry.GetValue(SecretKeyField)

            registry.Close()

            if isNull value then 
                failwith <| sprintf "%s value not found" SecretKeyField

            downcast value : string

type IAccountService =

    abstract member GetAll : unit -> Account [];

    abstract member Upsert : Account -> unit;

    abstract member Delete : string -> unit;
    
    abstract member Authenticate : string -> bool;

type AccountService() =
     
    interface IAccountService with

        member __.Authenticate masterPsw =
            let registry = getRootRegistry()
            let value = registry.GetValue(MasterPswField) :?> string

            not <| String.IsNullOrEmpty(value) && value = masterPsw
 
        member __.GetAll () =
            let registry = getRootRegistry()

            let getValue (registry: RegistryKey) name =
                let value = registry.GetValue(name)
                if isNull value then 
                    String.Empty 
                else value.ToString()

            let toAccount (name: string) =
                let childRegistry = registry.OpenSubKey(name)
                
                { Name = name
                  Key = getValue childRegistry "Key"
                  Psw = getValue childRegistry "Psw"
                  Desc = getValue childRegistry "Desc" }
               
              
            let accounts = 
                registry.GetSubKeyNames() 
                |> Array.map toAccount

            registry.Close() 

            accounts
            
        member __.Delete name =
            let registry = getRootRegistry()

            registry.DeleteSubKeyTree(name)
            registry.Close()

        member __.Upsert account =
            let upsert account =

                let delete (registry: RegistryKey) key =
                    let value = registry.GetValue(key)
                    if not <| isNull value then
                        registry.DeleteValue(key)
        
                let setValues (registry: RegistryKey) =
                    if String.IsNullOrEmpty(account.Key) then
                        delete registry "Key"
                    else registry.SetValue("Key", account.Key)

                    if String.IsNullOrEmpty(account.Psw) then
                        delete registry "Psw"
                    else registry.SetValue("Psw", account.Psw)

                    if String.IsNullOrEmpty(account.Desc) then
                        delete registry "Desc"
                    else registry.SetValue("Desc", account.Desc)

                    registry.Close()

                let rootRegistry = getRootRegistry()
                let registry = rootRegistry.OpenSubKey(account.Name, true)

                if isNull registry then
                    setValues <| rootRegistry.CreateSubKey(account.Name)
                else setValues registry
            
                rootRegistry.Close()

            let toDomain account =
        
                let invalidLength (value: string)=
                    if value.Length > 255 then true else false
            
                let tooLong x = sprintf "%s cannot be longer then 255 chars" x
            
                let validateName account = 
                    if String.IsNullOrEmpty(account.Name) then
                        TwoTrack.fail <| sprintf "Account name is required"
                    elif invalidLength account.Name then
                        TwoTrack.fail <| tooLong "Account name"
                    else TwoTrack.succeed account

                let validateKey account =
                    if String.IsNullOrEmpty(account.Key) then
                        TwoTrack.succeed { account with Key = "" }
                    elif invalidLength account.Key then
                       TwoTrack.fail <| tooLong "Key"
                    else TwoTrack.succeed account

                let validatePsw account =
                    if String.IsNullOrEmpty(account.Psw) then
                        TwoTrack.succeed { account with Psw = "" }
                    elif invalidLength account.Psw then
                        TwoTrack.fail <| tooLong "Psw"
                    else TwoTrack.succeed account

                let validateDesc account =
                    if String.IsNullOrEmpty(account.Desc) then
                        TwoTrack.succeed { account with Desc = "" }
                    elif invalidLength account.Desc then
                        TwoTrack.fail <| tooLong "Description"
                    else TwoTrack.succeed account

                let validateAccount =
                    validateName 
                    >=> validateKey 
                    >=> validatePsw 
                    >=> validateDesc

                match validateAccount account with
                | Failure e -> Failure e
                | Success _ -> Success account

            let tryUpsert =
                TwoTrack.tryCatch (TwoTrack.tee upsert) (fun ex -> ex.ToInnerMessage())

            let throwException message =
                failwith <| sprintf "Failed to save / update account: %s" message

            let save =
                toDomain
                >> TwoTrack.bind tryUpsert
                >> TwoTrack.valueOr throwException

            save account |> ignore