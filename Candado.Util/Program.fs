﻿open System
open ROP.Toolkit
open ROP.Toolkit.Operators
open Candado.Core

type Args = { SecretKey: string; MasterPsw: string; }

let rec parseCmdLine args argsSoFar =
    match args with
    | [] -> argsSoFar
    | "--SecretKey"::tail ->
        let newArgs = { argsSoFar with SecretKey = tail.Head }
        parseCmdLine tail.Tail newArgs
    | "--MasterPsw"::tail ->
        let newArgs = { argsSoFar with MasterPsw = tail.Head }
        parseCmdLine tail.Tail newArgs
    | _ -> argsSoFar

let validateArgs args =
    if String.IsNullOrEmpty(args.SecretKey) then
        Rop.fail "Please provide --SecretKey <secret key>"
    elif String.IsNullOrEmpty(args.MasterPsw) then
        Rop.fail "Please provide --MasterPsw <master password>"
    else Rop.succeed args

[<EntryPoint>]
let main argv = 
    try
        let args       = argv |> Array.toList
        let argsSoFar  = { SecretKey = ""; MasterPsw = "" }
        let parsedArgs = parseCmdLine args argsSoFar
        
        let init args =
            let service = AccountService() :> IAccountService
            service.Init args.SecretKey args.MasterPsw
            
        let execute =
            validateArgs
            >=> Rop.switch init
            >> Rop.valueOrDefault (fun x -> printf "%s" x)
        
        execute parsedArgs
        printf "Registry has been updated!"
    with
        ex -> printfn "%s" <| ex.ToInnerMessage()
    0 //! return an integer exit code