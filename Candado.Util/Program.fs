open ResultExtensions
open Candado.Core

type Args = { 
    SecretKey: string 
    Password: string
}

let rec parseCmdLine args argsSoFar =
    match args with
    | [] -> argsSoFar
    | "--SecretKey"::chunk ->
        let newArgs = { 
            argsSoFar with SecretKey = chunk.Head 
        }
        parseCmdLine chunk.Tail newArgs
    | "--Password"::chunk ->
        let newArgs = { 
            argsSoFar with Password = chunk.Head 
        }
        parseCmdLine chunk.Tail newArgs
    | _ -> argsSoFar
    
let parseArgs argv =
    let args = argv |> Array.toList

    let argsSoFar = { 
        SecretKey = ""
        Password = "" 
    }

    parseCmdLine args argsSoFar 

let validateArgs args =
    if args.SecretKey = "" then
        Error "Please provide --SecretKey"
    elif args.Password = "" then
        Error "Please provide --Password"
    else Ok args

[<EntryPoint>]
let main argv = 

    let log message =
        printfn "%s" message
    
    let init args =
        let secretKey = mapSecretKey args.SecretKey
        let password = mapPassword args.Password
        let result = RegEdit.initialize secretKey password

        match result with
            | Ok _ -> log "Storage Initialized!"
            | Error error ->
                match error with
                    | RegEdit.SecretKeyExits -> log "Secret Key already exists"
                    | RegEdit.MasterPasswordExists -> log "Master Password already exists"
           
    parseArgs argv
    |> validateArgs
    |> Result.map init
    |> Result.valueOr log
    
    0 //! exit code
