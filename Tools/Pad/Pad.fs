module PSX
open System
open System.Runtime.InteropServices
open FSharp.NativeInterop
open Microsoft.FSharp.NativeInterop
//let pad = new MailboxProcessor<int>(fun inbox -> 
//    let rec loop delay = async{
//        let! _ = inbox.TryReceive delay
//        let good = setupPad()
//        let good = true
//        if good then
//            match readPad() with
//            | Some (AnalogueRed data)-> 
//                printer.Post "analog data recvd"
//                for x in data do printHex x
//                return! loop delay
//            | Some (Digital data) -> 
//                printer.Post "digital data recvd"
//                return! loop delay
//            | Some config -> 
//                printer.Post "config data recvd"
//                return! loop delay
//            | None ->  
//                //printer.Post "failed to read data"
//                return! loop (delay)
//        else return! loop(delay)
//    }
//    loop 150)

let pad = new MailboxProcessor<int>(fun inbox -> 
    let rec loop delay = async{
        
        let! _ = inbox.TryReceive delay        
        match readPad() with
        | Some (AnalogueRed data)-> 
       //     printer.Post "analog data recvd"
       //     for x in data do printHex x
            return! loop delay
        | Some (Digital data) -> 
      //      printer.Post "digital data recvd"
            return! loop delay
        | Some config -> 
       //     printer.Post "config data recvd"
            return! loop delay
        | None ->  
            //printer.Post "failed to read data"
            return! loop (delay)        
    }
    
    loop 5)


[<EntryPoint>]
let main args =
    bcm2835_init() |> ignore
    printfn "Enabling SPI.."
    let res = bcm2835_spi_begin()
    fsel ATT true
    printfn "Enabled SPI.."  
    printfn "%A" res
    bcm2835_spi_setClockDivider(uint16 512)    
    bcm2835_spi_setDataMode(uint8 2)
//    pad.Start()
    let mutable continueLooping = true
    while continueLooping do
        if Console.ReadLine() = "a" then
            match readPad() with
            | Some(d) -> printfn "data!"
            | None -> printfn "no data"
        elif Console.ReadLine() = "x" then
            continueLooping <- false

    Console.ReadLine()
    printfn "Disabling SPI.."
    let res2 = bcm2835_spi_end()
    printfn "%A" res2   
    0
