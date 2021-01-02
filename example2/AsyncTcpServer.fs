// From c# example on https://docs.microsoft.com/ko-kr/dotnet/framework/network-programming/asynchronous-server-socket-example
namespace AsyncTcpServer

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading





type State() =
    // Size of receive buffer.
    static member BufferSize = 1024 
    
     // Receive buffer.  
    member val buffer : byte [] = Array.zeroCreate State.BufferSize with get,set
    
    // Received data string.
    member val sb = StringBuilder()
    
    // Client socket.
    member val work_socket : Socket = null with get,set
    
    



    
    
    
type AsyncTcpServer() =

    let all_done = new ManualResetEvent(false);
    let mutable listener : Socket = null

    
    let onSend(async_result : IAsyncResult) =
        let handler = async_result.AsyncState :?> Socket
        let byteSent = handler.EndSend(async_result)
        printfn "Sent {%d} bytes to client." byteSent
        
    
    
    let send(handler : Socket, data : String) =
        let byteData = Encoding.ASCII.GetBytes(data)
        handler.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, AsyncCallback(onSend), handler)
        
        
        
    
    let rec onRead(async_result : IAsyncResult) =
        let state = async_result.AsyncState :?> State
        let handler = state.work_socket
        
        let bytesRead = handler.EndReceive(async_result)
        if bytesRead > 0 then
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead)) |> ignore
            // Check for end-of-file tag. If it is not there, read
            // more data.             
            let content = state.sb.ToString()
            if content.IndexOf("<EOF>") > -1 then
                // All the data has been read from the
                // client. Display it on the console.                  
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content )
                
                // Echo the data back to the client. 
                send(handler, content) |> ignore
                
                // Clear buffer
                state.sb.Clear() |> ignore
            
            // Not all data received. Get more. 
            handler.BeginReceive(state.buffer, 0, State.BufferSize, SocketFlags.None, AsyncCallback(onRead), state) |> ignore

    
    
    
    
    let onAccept(async_result : IAsyncResult) =
        
        // Signal the main thread to continue. 
        all_done.Set() |> ignore
        
        // Get the socket that handles the client request.  
        let listener = async_result.AsyncState :?> Socket
        let handler = listener.EndAccept(async_result)
          
        // Create the state object.   
        let state = State()
        state.work_socket <- handler
        handler.BeginReceive(state.buffer, 0, State.BufferSize, SocketFlags.None, AsyncCallback(onRead), state) |> ignore
    
    
    
    
    let rec processAcceptAsync() =
        async {
            // Set the event to non-signaled state.  
            all_done.Reset() |> ignore
            
            // Start an asynchronous socket to listen for connections.
            Console.WriteLine("Waiting for a connection ...")
            listener.BeginAccept(AsyncCallback(onAccept), listener) |> ignore
            
            // Wait until a connection is made before continuing.  
            all_done.WaitOne() |> ignore
            
            do! processAcceptAsync()
        }
        
    
    
    member _.Start(port : int32) =
        let listen_adrs = IPAddress.Any
        let end_point = IPEndPoint(listen_adrs, port)
        
        // Create a TCP/IP socket.  
        listener <- new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);          
        
         // Bind the socket to the local endpoint and listen for incoming connections. 
        listener.Bind(end_point)
        listener.Listen(SocketOptionName.MaxConnections |> int32)

        Async.Start(processAcceptAsync(), cancellationToken = CancellationToken.None)
