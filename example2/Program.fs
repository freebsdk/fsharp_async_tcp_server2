// Original C# source code : https://docs.microsoft.com/ko-kr/dotnet/framework/network-programming/asynchronous-server-socket-example
// The entire C# code has been ported to F# by freebsdk 
open System
open AsyncTcpServer





[<EntryPoint>]
let main argv =
    let tcp_server = AsyncTcpServer()
    tcp_server.Start 9090
    printfn "Press any to terminate server ..."
    Console.ReadKey() |> ignore
    0 // return an integer exit code
