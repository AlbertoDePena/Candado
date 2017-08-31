namespace Candado.Core

open System
open Microsoft.Win32
open ROP.Micro
open System.Data.SqlClient
open System.Configuration
open System.Text

type ISecretKeyProvider =
    abstract member GetSecretKey : unit -> string;

type SecretKeyProvider () =
    [<Literal>]
    let RegistryKey = "Software\Candado"

    [<Literal>]
    let RegistryField = "SecretKey"
    
    interface ISecretKeyProvider with 
        member __.GetSecretKey () =
            let registryKey = Registry.CurrentUser.OpenSubKey(RegistryKey)
            if isNull registryKey 
            then failwith (sprintf "Registry key '%s' with field '%s' has no value" RegistryKey RegistryField)
            else registryKey.GetValue(RegistryField).ToString()
            
type IAccountService =
    abstract member GetAll : unit -> Account []
    abstract member SaveAll : Account[] -> unit

type AccountService () =
    [<Literal>]
    let BaseQuery = "SELECT [Id],[Name],[UserName],[Password],[Description] FROM [dbo].[Account]"

    let getDbConnString() =
        ConfigurationManager.ConnectionStrings.["Candado:DbConnString"].ConnectionString;

    let validateDbConnString dbConnString =
        if String.IsNullOrEmpty(dbConnString) then
            fail "DB connection string is empty"
        else succeed dbConnString
        
    let rec readAsync (reader: SqlDataReader) (mapFunc: SqlDataReader -> 'T) (list: System.Collections.Generic.List<'T>) =
        async {
            let! cont = Async.AwaitTask(reader.ReadAsync())
            if cont then
                reader |> mapFunc |> list.Add
                return! readAsync reader mapFunc list
            else return list
        }

    let executeQuery (dbConnString: string, query: string) =
        async {
            use conn = new SqlConnection(dbConnString)
            let! connected = Async.AwaitIAsyncResult(conn.OpenAsync())
            if not <| connected then failwith "Failed to connect to database"
            use cmd = new SqlCommand(query, conn)
            return cmd.ExecuteNonQuery()
        }

    let executeQueryWithResult (dbConnString: string, query: string) =
        let mapFunc (reader: SqlDataReader) =
            { Id = reader.GetInt32(0); 
              Name = reader.GetString(1); 
              UserName = reader.GetString(2); 
              Password = reader.GetString(3);
              Description = reader.GetString(4); }

        async {
            use conn = new SqlConnection(dbConnString)
            let! connected = Async.AwaitIAsyncResult(conn.OpenAsync())
            if not <| connected then failwith "Failed to connect to database"
            use cmd = new SqlCommand(query, conn)
            use! reader = Async.AwaitTask(cmd.ExecuteReaderAsync())
            let! result = readAsync reader mapFunc (System.Collections.Generic.List<Account>())
            return result.ToArray()
        }
        
    interface IAccountService with
        member __.GetAll() =
            let withQuery dbConnString =
                (dbConnString, BaseQuery)
                
            let tryExecuteQuery =
                tryCatch executeQueryWithResult (fun ex -> (sprintf "Failed to get accounts: %s" ex.Message))

            let getAccounts =
                validateDbConnString
                >> map withQuery
                >> bind tryExecuteQuery

            match getDbConnString() |> getAccounts with
            | Success task -> Async.RunSynchronously(task)
            | Failure error -> failwith error

        member __.SaveAll accounts =
            let stringBuilder = StringBuilder("BEGIN")

            let buildQuery account =
                stringBuilder.AppendLine(String.Format(@" IF (NOT EXISTS (SELECT [Id] FROM [dbo].[Account] WHERE [Id] = {0}))
	                                        BEGIN INSERT INTO [dbo].[Account] ([Name],[UserName],[Password],[Description]) VALUES ('{1}','{2}','{3}','{4}')
	                                        END ELSE BEGIN
		                                        UPDATE [dbo].[Account] SET [Name] = '{1}', [UserName] = '{2}', [Password] = '{3}', [Description] = '{4}'
		                                        WHERE [Id] = {0} END", account.Id, account.Name, account.UserName, account.Password, account.Description)) |> ignore

            accounts |> Array.iter buildQuery

            let query = stringBuilder.AppendLine("END").ToString()

            let withQuery dbConnString =
                (dbConnString, query)

            let tryExecuteQuery =
                tryCatch executeQuery (fun ex -> (sprintf "Failed to get accounts: %s" ex.Message))

            let saveAccounts =
                validateDbConnString
                >> map withQuery
                >> bind tryExecuteQuery
            
            match getDbConnString() |> saveAccounts with
            | Success task -> Async.RunSynchronously(task) |> ignore
            | Failure error -> failwith error

                
                
 