namespace Candado.Core

open System
open Microsoft.Win32
open ROP.Toolkit
open ROP.Toolkit.Operators

type IAccountService =
    abstract member GetSecretKey : unit -> string;

    abstract member GetAccounts : unit -> Account [];

    abstract member SaveAccount : Account -> unit;

    abstract member DeleteAccount : string -> unit;

    abstract member Init : string -> string -> unit;

    abstract member Authenticate : string -> bool;

type AccountService() =

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
        
    interface IAccountService with

        member __.Authenticate masterPsw =
            let registry = getRootRegistry()
            let value    = registry.GetValue(MasterPswField) :?> string

            not <| String.IsNullOrEmpty(value) && value = masterPsw

        member __.Init (secretKey: string) (masterPsw: string) =
            let validateArgs (secretKey, masterPsw) =
                if String.IsNullOrEmpty(secretKey) then
                    Rop.fail "Secret key is required"
                elif String.IsNullOrEmpty(masterPsw) then
                    Rop.fail "Master password is required"
                else Rop.succeed (secretKey, masterPsw)

            let getCandadoRegistry () =
                let registry = Registry.CurrentUser.OpenSubKey(CandadoField, true)
                if isNull registry then
                    Registry.CurrentUser.CreateSubKey(CandadoField)
                else registry
            
            let setSecretKeyRegistry (root: RegistryKey) (secretKey, masterPsw) =
                let value = root.GetValue(SecretKeyField) :?> string
                if not <| String.IsNullOrEmpty(value) then
                    printfn "Secret key has already been set"
                else root.SetValue(SecretKeyField, secretKey)
               
                Rop.succeed (secretKey, masterPsw)

            let setMasterPswRegistry (root: RegistryKey) (secretKey, masterPsw) =
                let value = root.GetValue(MasterPswField) :?> string
                if not <| String.IsNullOrEmpty(value) then
                    printfn "Master password has already been set"
                else root.SetValue(MasterPswField, masterPsw)
                    
                Rop.succeed (secretKey, masterPsw)
            
            let registry = getCandadoRegistry()

            let execute =
                validateArgs
                >=> setSecretKeyRegistry registry
                >=> setMasterPswRegistry registry
                >> Rop.valueOrDefault (fun ex -> failwith ex)
            
            execute (secretKey, masterPsw) |> ignore

            registry.Close()
            
        member __.GetAccounts () =
            let registry = getRootRegistry()

            let getValue (registry: RegistryKey) name =
                let value = registry.GetValue(name)

                if isNull value then 
                    String.Empty 
                else value.ToString()

            let toAccount (name: string) =
                let childRegistry = registry.OpenSubKey(name)
                
                { Name  = name; 
                  Key   = getValue childRegistry "Key";
                  Token = getValue childRegistry "Token";
                  Desc  = getValue childRegistry "Desc"; }
               
              
            let accounts = 
                registry.GetSubKeyNames() 
                |> Array.map toAccount

            registry.Close() 

            accounts

        member __.GetSecretKey () =
            let registry = getRootRegistry()

            let value = registry.GetValue(SecretKeyField)

            registry.Close()

            if isNull value then 
                failwith <| sprintf "%s value not found" SecretKeyField

            value.ToString()
                

        member __.DeleteAccount name =
            let registry = getRootRegistry()

            registry.DeleteSubKeyTree(name)
            registry.Close()

        member __.SaveAccount account =
            let upsert account =

                let delete (registry: RegistryKey) key =
                    let value = registry.GetValue(key)
                    if not <| isNull value then
                        registry.DeleteValue(key)
        
                let setValues (registry: RegistryKey) =
                    if String.IsNullOrEmpty(account.Key) then
                        delete registry "Key"
                    else registry.SetValue("Key", account.Key)

                    if String.IsNullOrEmpty(account.Token) then
                        delete registry "Token"
                    else registry.SetValue("Token", account.Token)

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

                let validateToken account =
                    if String.IsNullOrEmpty(account.Token) then
                        Rop.succeed { account with Token = "" }
                    elif invalidLength account.Token then
                        Rop.fail <| tooLong "Token"
                    else Rop.succeed account

                let validateDescription account =
                    if String.IsNullOrEmpty(account.Desc) then
                        Rop.succeed { account with Desc = "" }
                    elif invalidLength account.Desc then
                        Rop.fail <| tooLong "Description"
                    else Rop.succeed account

                let validateAccount =
                    validateName 
                    >=> validateKey 
                    >=> validateToken 
                    >=> validateDescription

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