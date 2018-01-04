open System
open Tachyon.Core
open Tachyon.Actors
open System.Threading.Tasks

let runtime = ActorRuntime.Start()

let rec behavior cell state msg = Actor.become (state+msg) (fun c s m -> behavior c s m |> ValueTask)
let aref = runtime.Spawn(Actor.immutable 0 behavior, "counter")

aref <! 1

Console.ReadLine() |> ignore