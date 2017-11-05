module Tachyon.IO.Tcp

open System
open System.Net
open System.Threading.Tasks
open Tachyon.Streams

type IncomingConnection = 
    { Address: IPEndPoint }

type ServerConfig = 
    { Address: EndPoint }
    
type ServerBinding = { RemoteAddress: EndPoint }

let server host endpoint : Source<IncomingConnection, Task<ServerBinding>> = failwith "not implemented"