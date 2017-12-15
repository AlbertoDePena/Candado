open System
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
    
let validateArgs args =
    if args.SecretKey = "" then
        Error "Please provide --SecretKey"
    elif args.Password = "" then
        Error "Please provide --Password"
    else Ok args

[<EntryPoint>]
let main argv = 

    let log message =
        printfn "%s" message; message
    
    let args = argv |> Array.toList

    let argsSoFar = { 
        SecretKey = ""
        Password = "" 
    }
        
    let init args =
        match RegEditInitializer.Initialize args.SecretKey args.Password with
            | Ok _ -> Ok "Storage Initialized!"
            | Error error ->
                match error with
                    | RegEditInitializer.SecretKeyExits -> Error "Secret Key already exists"
                    | RegEditInitializer.MasterPasswordExists -> Error "Master Password already exists"
            
    let execute =
        validateArgs
        >> Result.bind init
        >> Result.valueOr log 
        >> ignore
        
    parseCmdLine args argsSoFar 
    |> execute
    0 //! return an integer exit code
