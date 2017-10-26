open System
open ROP.Toolkit
open ROP.Toolkit.Operators
open Candado.Core

type SecretKey = SecretKey of string

type MasterPsw = MasterPsw of string

type Args = { 
    SecretKey: SecretKey 
    MasterPsw: MasterPsw
}

let rec parseCmdLine args argsSoFar =
    match args with
    | [] -> argsSoFar
    | "--SecretKey"::chunk ->
        let newArgs = { 
            argsSoFar with SecretKey = SecretKey chunk.Head 
        }
        parseCmdLine chunk.Tail newArgs
    | "--MasterPsw"::chunk ->
        let newArgs = { 
            argsSoFar with MasterPsw = MasterPsw chunk.Head 
        }
        parseCmdLine chunk.Tail newArgs
    | _ -> argsSoFar
    
let validateArgs args =
    if args.SecretKey = (SecretKey "") then
        Rop.fail "Please provide --SecretKey <secret key>"
    elif args.MasterPsw = (MasterPsw "") then
        Rop.fail "Please provide --MasterPsw <master password>"
    else Rop.succeed args

[<EntryPoint>]
let main argv = 
    let log message =
        printfn "%s" message
    
    try
        let args = argv |> Array.toList

        let argsSoFar = { 
            SecretKey = SecretKey ""
            MasterPsw = MasterPsw "" 
        }
        
        let init args =
            let (SecretKey secretKey) = args.SecretKey
            let (MasterPsw masterPsw) = args.MasterPsw

            RegistryHelper.Init secretKey masterPsw
            
        let execute =
            validateArgs
            >=> Rop.switch init
            >> Rop.valueOr log
        
        execute <| parseCmdLine args argsSoFar

        log "Done!"
    with
        ex -> ex.ToInnerMessage() |> log
    0 //! return an integer exit code
