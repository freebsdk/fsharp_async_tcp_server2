open System
open AsyncTcpServer





[<EntryPoint>]
let main argv =
    let tcp_server = AsyncTcpServer()
    tcp_server.Start 9090
    printfn "Press any to terminate server ..."
    Console.ReadKey() |> ignore
    0 // return an integer exit code
