namespace Candado.Core

module ROP =

    type Result<'TSuccess, 'TFail> =
    | Success of 'TSuccess
    | Fail of 'TFail

    let bind func input =
        match input with
        | Success s -> func s
        | Fail f -> Fail f

    let (>>=) func input =
        bind func input

    let switch func1 func2 input =
        match func1 input with
        | Success s -> func2 s
        | Fail f-> Fail f

    let (>=>) func1 func2 input =
        switch func1 func2 input

    let map func input =
            fun input ->
                match input with
                | Success s -> Success (func s)
                | Fail f -> Fail f

    let tee func input =
            func input
            input

    let fail input =
        Fail input

    let success input =
        Success input
