namespace Candado.Core

type Account =
    {
        Name: string; 
        Key: string; 
        Token: string; 
        Desc: string; 
    }

[<AutoOpen>]
module Extensions =

    type System.Exception with 
        member this.ToInnerMessage() =
            if isNull this.InnerException then
                this.Message 
            else this.InnerException.ToInnerMessage()

    let GetInnerMessage (e: System.Exception) =
        e.ToInnerMessage()