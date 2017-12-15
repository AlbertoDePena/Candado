namespace Candado.Core

[<AutoOpen>]
module Dtos =

    type AccountDto = {
        AccountName: string
        UserName: string
        Password: string
        Description: string
    }

[<RequireQualifiedAccess>]
module String255 =
    open System

    type T = private String255 of string

    type StringError =
        | StringIsNullOrEmpty
        | StringLength of int
        
    let private createWithContinuation ok error (str: string) =
        if String.IsNullOrEmpty(str) then
            error StringIsNullOrEmpty
        elif str.Length > 255 then
            StringLength 255 |> error
        else String255 str |> ok
        
    let createOptional str =
        let ok x = Some x
        let error _ = None

        createWithContinuation ok error str
        
    let create (str: string) =
        let ok x = Ok x
        let error e = Error e

        createWithContinuation ok error str

    let apply f (String255 x) = f x

    let value x = apply id x

    let fail propertyName error =
        match error with
            | StringIsNullOrEmpty -> sprintf "%s is required" propertyName |> invalidOp
            | StringLength length -> sprintf "%s cannot be longer than %i" propertyName length |> invalidOp

[<AutoOpen>]
module DataTypes =
    open System
    open Microsoft.Win32

    type Exception with 
        member this.ToInnerMessage() =
            if isNull this.InnerException then
                this.Message 
            else this.InnerException.ToInnerMessage()
            
    type AccountName = AccountName of String255.T

    type Password = Password of String255.T
    
    type SecretKey = SecretKey of String255.T

    type PlainText = PlainText of string

    type EncryptedText = EncryptedText of string

    type DecryptedText = DecryptedText of string
    
    type CandadoError =
        | RootRegistryKeyAccess
        | RetrieveSecretKey
        | RetrieveMasterPassword
        | DeleteAccount of string
        | UpsertAccount of string
        | GetAccounts of string
        | RequiredProperty of string
        | PropertyLength of string * int
        | CryptoFailure of string
        | InvalidPassword
        
    let fail error =
        let message = 
            match error with
                | RootRegistryKeyAccess -> "Failed to access storage. Make sure storage is initialized"
                | RetrieveMasterPassword -> "Failed to get master password from storage"
                | RetrieveSecretKey -> "Failed to get secret key from storage"
                | GetAccounts message -> sprintf "Failed to get accounts: %s" message
                | DeleteAccount message -> sprintf "Failed to deleted account: %s" message
                | UpsertAccount message -> sprintf "Failed to upsert account: %s" message
                | RequiredProperty name -> sprintf "%s is requried" name
                | PropertyLength (name, length) -> sprintf "%s cannot be longer than %i" name length
                | CryptoFailure message -> sprintf "Cryptography error: %s" message
                | InvalidPassword -> "Invalid password"

        invalidOp message
        
    let mapProperty value propertyName dataType =
        match String255.create value with
            | Error error -> String255.fail propertyName error
            | Ok value' -> dataType value'

    let mapPassword value =
        mapProperty value "Password" Password

    let mapAccountName value =
        mapProperty value "AccountName" AccountName

    let mapSecretKey value =
        mapProperty value "Secret Key" SecretKey

    type Account = {
        AccountName: String255.T 
        UserName: String255.T option
        Password: String255.T option
        Description: String255.T option
    }
    
    let GetInnerMessage (e: Exception) =
        e.ToInnerMessage()
    
[<RequireQualifiedAccess>]
module RegEdit =
    open Microsoft.Win32
    open DataTypes

    let [<Literal>] RootField = "Software\Candado"

    let [<Literal>] PasswordField = "MasterPassword"

    let [<Literal>] SecretKeyField = "SecretKey"

    let private abort (key: RegistryKey) error =
        key.Close()
        Error error
        
    let connect password =
        
        let getRoot () =
            let root = Registry.CurrentUser.OpenSubKey(RootField, true) 

            if isNull root then
                Error RootRegistryKeyAccess
            else Ok root

        let getPassword (key: RegistryKey) =
            let value = key.GetValue(PasswordField)
            
            if isNull value then
                abort key RetrieveMasterPassword
            else 
                let value' = (downcast value : string)

                match String255.create value' with
                    | Ok password -> Ok (password, key)
                    | Error _ -> abort key RetrieveMasterPassword
                     
        let verifyPassword inputPassword (masterPassword, key: RegistryKey) =
            if inputPassword <> masterPassword then
                abort key InvalidPassword
            else Ok key

        let (Password psw) = password

        getRoot()
        |> Result.bind getPassword
        |> Result.bind (verifyPassword psw)
        
    let disconnect (x, registryKey: RegistryKey) =
        registryKey.Close(); x
            
    let getSecretKey (registryKey: RegistryKey) =

        let getValue field (key: RegistryKey) =
            let value = key.GetValue(field)
            let abort' () = abort key RetrieveSecretKey
        
            if isNull value then
                abort' ()
            else
                let value' = (downcast value : string)

                match String255.create value' with
                    | Ok value'' -> (SecretKey value'', key) |> Ok
                    | Error _ -> abort' ()

        getValue SecretKeyField registryKey
        
[<RequireQualifiedAccess>]
module internal Storage =
    open DataTypes
    open Microsoft.Win32
    open ResultExtensions
    
    let deleteAccount password accountName =
        
        let tryDelete accountName =
            let (AccountName name) = accountName
            
            let delete (key: RegistryKey) =
                let result = key.DeleteSubKeyTree(String255.value name)
                (result, key)

            let toError (ex: exn) =
                ex.ToInnerMessage() |> DeleteAccount

            Result.tryCatch delete toError
                
        password
        |> RegEdit.connect
        |> Result.bind (tryDelete accountName)
        |> Result.map RegEdit.disconnect

    let getSecretKey password =
        
        password
        |> RegEdit.connect 
        |> Result.bind RegEdit.getSecretKey
        |> Result.map RegEdit.disconnect
                        
    let getAccounts password = 

        let tryGetAccounts =
            
            let getAccounts (key: RegistryKey) =

                let getValue (childKey: RegistryKey) propertyName =
                    let value = childKey.GetValue(propertyName)

                    if isNull value then 
                        System.String.Empty 
                    else downcast value : string

                let toAccount (name: string) : Account =
                    let childKey = key.OpenSubKey(name)
                    let userName = getValue childKey "UserName"
                    let password = getValue childKey "Password"
                    let description = getValue childKey "Description"
                    
                    { AccountName = 
                        match String255.create name with
                            | Error error -> String255.fail "Account Name" error
                            | Ok value -> value
                      UserName = String255.createOptional userName
                      Password = String255.createOptional password
                      Description = String255.createOptional description }

                let accounts =
                    key.GetSubKeyNames() 
                    |> Array.map toAccount
                    
                (accounts, key)

            let toError (ex: exn) =
                ex.ToInnerMessage() |> GetAccounts

            Result.tryCatch getAccounts toError
            
        RegEdit.connect password
        |> Result.bind tryGetAccounts
        |> Result.map RegEdit.disconnect

    let upsertAccount password account = 
        
        let tryUpsert account =
            
            let upsert (key: RegistryKey) =

                let setOrRemove (childKey: RegistryKey) propertyName valueOpt =

                    let deleteValue key =
                        let value = childKey.GetValue(key)
                        if isNull value |> not then
                            childKey.DeleteValue(key)

                    match valueOpt with
                        | Some x -> 
                            let value = String255.value x
                            childKey.SetValue(propertyName, value)
                        | None -> deleteValue propertyName
                        
                let setValues (child: RegistryKey) =
                    setOrRemove child "UserName" account.UserName
                    setOrRemove child "Password" account.Password
                    setOrRemove child "Description" account.Description
                
                let accountName = String255.value account.AccountName
                let childKey = key.OpenSubKey(accountName, true)

                if isNull childKey then
                    key.CreateSubKey(accountName) |> setValues
                else setValues childKey

                ((), key)

            let toError (ex: exn) =
                ex.ToInnerMessage() |> UpsertAccount

            Result.tryCatch upsert toError
                    
        RegEdit.connect password
        |> Result.bind (tryUpsert account)
        |> Result.map RegEdit.disconnect
            
[<RequireQualifiedAccess>]
module internal Crypto =
    open System.IO
    open System.Text
    open System.Security.Cryptography
    open DataTypes

    let private tryExecute func =
        try
            func()
        with
            | ex -> ex.ToInnerMessage() |> CryptoFailure |> Error

    let private transform key text getTransform getBytes transformArray =

        let getCryptoProvider (SecretKey key) =
            use md5 = new MD5CryptoServiceProvider()
            let des = new TripleDESCryptoServiceProvider()
            let size = des.BlockSize / 8
            let bytes = Encoding.Unicode.GetBytes(String255.value key)

            des.Key <- md5.ComputeHash(bytes)
            des.IV <- Array.zeroCreate (size)
            des

        use stream = new MemoryStream()
        let transform = getCryptoProvider key |> getTransform
        use cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write)
        let bytes = getBytes text

        cryptoStream.Write(bytes, 0, bytes.Length)
        cryptoStream.FlushFinalBlock()
        stream.ToArray() |> transformArray
    
    let decrypt secretKey text : Result<DecryptedText, CandadoError> =

        let func () =
            let getTransform (des: TripleDESCryptoServiceProvider) = 
                des.CreateDecryptor()

            let getBytes (EncryptedText text) = 
                System.Convert.FromBase64String(text)

            let transformArray array = 
                Encoding.Unicode.GetString(array) |> DecryptedText

            transform secretKey text getTransform getBytes transformArray |> Ok

        tryExecute func

    let encrypt secretKey text : Result<EncryptedText, CandadoError> =

        let func () =
            let getTransform (des: TripleDESCryptoServiceProvider) = 
                des.CreateEncryptor()
        
            let getBytes (PlainText text) = 
                Encoding.Unicode.GetBytes(text) 

            let transformArray array = 
                System.Convert.ToBase64String(array) |> EncryptedText

            transform secretKey text getTransform getBytes transformArray |> Ok

        tryExecute func