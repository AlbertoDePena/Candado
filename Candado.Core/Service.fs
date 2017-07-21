namespace Candado.Core

open System
open System.IO
open System.Configuration
open Newtonsoft.Json
open Microsoft.Win32
open Crypto.Service

type IAccountService =
    abstract member GetAll : unit -> Account []
    abstract member SaveAll : Account [] -> unit
    abstract member GetSecretKey : unit -> string

type AccountService(crypto: ICryptoService) =
    [<Literal>]
    let AppSettingKey = "Candado:StorageDirectory"

    [<Literal>]
    let FileName = "accounts.json"

    [<Literal>]
    let RegistryKey = "Software\Candado"

    [<Literal>]
    let RegistryField = "SecretKey"

    let getSecretKey() =
        let createRegistryKey =
            fun () ->
                let guid = Guid.NewGuid().ToString()
                let registryKey = Registry.CurrentUser.CreateSubKey(RegistryKey)
                registryKey.SetValue(RegistryField, guid)
                registryKey

        let registryKey = Registry.CurrentUser.OpenSubKey(RegistryKey)
        if isNull registryKey 
        then createRegistryKey().GetValue(RegistryField).ToString()
        else registryKey.GetValue(RegistryField).ToString()
        
    let getStorageDirectory() =
        let setting = ConfigurationManager.AppSettings.[AppSettingKey]
        if String.IsNullOrEmpty(setting) 
        then None
        else Some setting
            
    let getFilePath directoryOption =
        match directoryOption with
        | None -> None
        | Some directory ->
            let filePath = (sprintf "%s\%s" directory FileName)
            let result = Some filePath
            if File.Exists(filePath) then result
            else
                use writer = File.CreateText(filePath)
                writer.Write("[]")
                result

    let readAllText filePathOption =
        match filePathOption with
        | None -> None
        | Some filePath -> Some (File.ReadAllText(filePath))

    let deserialize textOption =
        match textOption with
        | None -> None
        | Some text -> Some (JsonConvert.DeserializeObject<Account []>(text))
    
    let getAccounts = 
        getStorageDirectory 
        >> getFilePath 
        >> readAllText 
        >> deserialize
    
    let fail() = 
        failwith "Failed to get accounts. Make sure storage directory exists."
        
    interface IAccountService with
        member this.GetAll() =
            let key = getSecretKey()
            let mapAccount =
                fun account -> 
                    if String.IsNullOrEmpty(account.Password) then account
                    else { account with Password = crypto.Decrypt(key, account.Password)}
            match getAccounts() with
            | None -> fail()
            | Some accounts -> accounts |> Array.map mapAccount

        member this.SaveAll(accounts: Account []) =
            let func = getStorageDirectory >> getFilePath
            match func() with
            | None -> fail()
            | Some filePath ->
                let key = getSecretKey()
                let mapAccount =
                    fun account -> 
                        if String.IsNullOrEmpty(account.Password) then account
                        else { account with Password = crypto.Encrypt(key, account.Password)}
                let json = accounts |> Array.map mapAccount |> JsonConvert.SerializeObject
                File.WriteAllText(filePath, json)

        member this.GetSecretKey() = getSecretKey()

                
                
 