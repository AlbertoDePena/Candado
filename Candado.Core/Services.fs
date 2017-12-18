namespace Candado.Core

open System
open Microsoft.Win32
open ResultExtensions
open DataTypes
  
type ICryptoService =
    abstract member Decrypt : string -> string -> string
    abstract member Encrypt : string -> string -> string
        
type IAuthenticationService =
    abstract member Authenticate : string -> bool

type IStorageService =
    abstract member GetSecretKey : string -> string
    abstract member GetAllAccounts : string -> AccountDto []
    abstract member UpsertAccount : string -> AccountDto -> unit
    abstract member DeleteAccount : string -> string -> unit

type CryptoService() =
    
    interface ICryptoService with

        member __.Decrypt key text =

            let encryptedText =
                if String.IsNullOrEmpty(text) then
                    invalidOp "Please provide text to decrypt"
                else (EncryptedText text)
                    
            let toString (DecryptedText x) = x

            Crypto.decrypt (mapSecretKey key) encryptedText
            |> Result.map toString
            |> Result.valueOr fail

        member __.Encrypt key text =

            let plainText =
                if String.IsNullOrEmpty(text) then
                    invalidOp "Please provide text to encrypt"
                else (PlainText text)

            let toString (EncryptedText x) = x

            Crypto.encrypt (mapSecretKey key) plainText
            |> Result.map toString
            |> Result.valueOr fail

type AuthenticationService() =

    interface IAuthenticationService with

        member __.Authenticate password =
            
            let disconnect (root: RegistryKey) =
                RegEdit.disconnect (Ok true, root)

            let psw = mapPassword password 

            RegEdit.connect psw
            |> Result.bind disconnect
            |> Result.valueOr fail
                
type StorageService() =
    
    interface IStorageService with

        member __.GetSecretKey password =
            
            let toString (SecretKey key) =
                String255.value key
            
            let psw = mapPassword password 

            Storage.getSecretKey psw
            |> Result.map toString
            |> Result.valueOr fail
            
        member __.GetAllAccounts password =

            let toAccountDtos accounts =

                let getValue valueOpt =
                    match valueOpt with
                        | None -> String.Empty
                        | Some x -> String255.value x
                    
                let toAccountDto (account: Account) : AccountDto = {
                    AccountName = String255.value account.AccountName
                    UserName = getValue account.UserName
                    Password = getValue account.Password
                    Description = getValue account.Description
                }

                accounts
                |> Array.map toAccountDto

            let psw = mapPassword password 

            Storage.getAccounts psw
            |> Result.map toAccountDtos
            |> Result.valueOr fail
            
        member __.DeleteAccount password accountName =
        
            let psw = mapPassword password 
            let name = mapAccountName accountName
        
            Storage.deleteAccount psw name
            |> Result.valueOr fail

        member __.UpsertAccount password account =

            let toModel (AccountName name) (dto: AccountDto) : Account = {
                AccountName = name
                UserName = String255.createOptional dto.UserName
                Password = String255.createOptional dto.Password
                Description = String255.createOptional dto.Description
            }
            
            let psw = mapPassword password 
            let acc = toModel (mapAccountName account.AccountName) account

            Storage.upsertAccount psw acc
            |> Result.valueOr fail