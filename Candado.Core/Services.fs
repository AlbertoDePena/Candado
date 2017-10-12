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
                    Rop.fail "Secret key is required"
                elif String.IsNullOrEmpty(masterPsw) then
                    Rop.fail "Master password is required"
                else Rop.succeed root

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
               
            Rop.succeed root

        let setMasterPswRegistry (root: RegistryKey) =
            let value = root.GetValue(MasterPswField) :?> string
            if not <| String.IsNullOrEmpty(value) then
                printfn "Master password has already been set"
            else root.SetValue(MasterPswField, masterPsw)
                    
            Rop.succeed root
            
        let execute =
            validateArgs
            >=> setSecretKeyRegistry
            >=> setMasterPswRegistry
            >> Rop.valueOrDefault (fun ex -> failwith ex)
            
        let registry = getCandadoRegistry()

        registry |> execute |> ignore

        registry.Close()

type ICryptoService =
    abstract member Decrypt : string -> string -> string
    abstract member Encrypt : string -> string -> string

type CryptoService() =

    let createDes (key: string) =
        use md5   = new MD5CryptoServiceProvider()
        let des   = new TripleDESCryptoServiceProvider()
        let bytes = Encoding.Unicode.GetBytes(key)

        des.Key <- md5.ComputeHash(bytes)
        des.IV  <- Array.zeroCreate (des.BlockSize / 8)
        des

    interface ICryptoService with

        member __.Decrypt key text =
            use stream       = new MemoryStream()
            let des          = createDes key
            use cryptoStream = new CryptoStream(stream, des.CreateDecryptor(), CryptoStreamMode.Write)
            let bytes        = Convert.FromBase64String(text)

            cryptoStream.Write(bytes, 0, bytes.Length)
            cryptoStream.FlushFinalBlock()

            let array = stream.ToArray()

            Encoding.Unicode.GetString(array)

        member __.Encrypt key text =
            use stream       = new MemoryStream()
            let des          = createDes key
            use cryptoStream = new CryptoStream(stream, des.CreateEncryptor(), CryptoStreamMode.Write)
            let bytes        = Encoding.Unicode.GetBytes(text)

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
            let value    = registry.GetValue(SecretKeyField)

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
            let value    = registry.GetValue(MasterPswField) :?> string

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
                
                { Name = name; 
                  Key  = getValue childRegistry "Key";
                  Psw  = getValue childRegistry "Psw";
                  Desc = getValue childRegistry "Desc"; }
               
              
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
                let registry     = rootRegistry.OpenSubKey(account.Name, true)

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
                        Rop.fail <| sprintf "Account name is required"
                    elif invalidLength account.Name then
                        Rop.fail <| tooLong "Account name"
                    else Rop.succeed account

                let validateKey account =
                    if String.IsNullOrEmpty(account.Key) then
                        Rop.succeed { account with Key = "" }
                    elif invalidLength account.Key then
                        Rop.fail <| tooLong "Key"
                    else Rop.succeed account

                let validatePsw account =
                    if String.IsNullOrEmpty(account.Psw) then
                        Rop.succeed { account with Psw = "" }
                    elif invalidLength account.Psw then
                        Rop.fail <| tooLong "Psw"
                    else Rop.succeed account

                let validateDesc account =
                    if String.IsNullOrEmpty(account.Desc) then
                        Rop.succeed { account with Desc = "" }
                    elif invalidLength account.Desc then
                        Rop.fail <| tooLong "Description"
                    else Rop.succeed account

                let validateAccount =
                    validateName 
                    >=> validateKey 
                    >=> validatePsw 
                    >=> validateDesc

                match validateAccount account with
                | Failure f -> Rop.fail f
                | Success _ -> Rop.succeed account

            let tryUpsert =
                Rop.tryCatch (Rop.tee upsert) (fun ex -> ex.ToInnerMessage())

            let save =
                toDomain
                >> Rop.bind tryUpsert
                >> Rop.valueOrDefault (fun e -> failwith <| sprintf "Failed to save / update account: %s" e)

            save account |> ignore