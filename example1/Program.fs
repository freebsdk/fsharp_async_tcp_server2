// Original source : http://www.fssnip.net/1E/title/Async-TCP-Server
// Some code has been modified due to type issues by freebsdk
open System
open System.Net
open System.Net.Sockets
open System.Threading





type Socket with
    member socket.AsyncAccept() =
        Async.FromBeginEnd(socket.BeginAccept, socket.EndAccept)

    
    member socket.AsyncReceive(buffer: byte [], ?offset, ?count) =
        let offset = defaultArg offset 0
        let count = defaultArg count buffer.Length

        let beginReceive (b, o, c, cb, s) =
            socket.BeginReceive(b, o, c, SocketFlags.None, cb, s)

        Async.FromBeginEnd(buffer, offset, count, beginReceive, socket.EndReceive)

    
    member socket.AsyncSend(buffer: byte [], ?offset, ?count) =
        let offset = defaultArg offset 0
        let count = defaultArg count buffer.Length

        let beginSend (b, o, c, cb, s) =
            socket.BeginSend(b, o, c, SocketFlags.None, cb, s)

        Async.FromBeginEnd(buffer, offset, count, beginSend, socket.EndSend)





type TcpServer() =
    
    static member Start(hostname: string, ?port: int32) =
        let ip_adrs = Dns.GetHostEntry(hostname).AddressList.[0]
        TcpServer.Start(ip_adrs, port)


    static member Start(?ip_adrs, ?port) =
        let listen_adrs = defaultArg ip_adrs IPAddress.Any
        let port = defaultArg port (Some 8080)
        let end_point = IPEndPoint(listen_adrs, port.Value)
        let cts = new CancellationTokenSource()

        let listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

        listener.Bind(end_point)
        listener.Listen(SocketOptionName.MaxConnections |> int32)
        printfn "Start listening on port %d" port.Value

        let rec loop () =
            async {
                printfn "Waiting for request.."

                let! socket = listener.AsyncAccept()
                printfn "Received request"

                let response =
                    [| "HTTP/1.1 200 OK\r\n"B
                       "Content-Type: text/plain\r\n"B
                       "\r\n"B
                       "Hello World!"B |]
                    |> Array.concat

                try
                    try
                        let! bytesSent = socket.AsyncSend(response)
                        printfn "Sent response %d" bytesSent
                    with e -> printfn "An error occurred: %s" e.Message
                finally
                    socket.Shutdown(SocketShutdown.Both)
                    socket.Close()

                return! loop ()
            }

        Async.Start(loop (), cancellationToken = cts.Token)

        { new IDisposable with
            member x.Dispose() =
                cts.Cancel()
                listener.Close() }





[<EntryPoint>]
let main argv =
    use disposable = TcpServer.Start()
    printfn "Press any to terminate server."
    Console.ReadKey() |> ignore
    0 // return an integer exit code
