[<AutoOpen>]
module PsxController

let ATT = (psxToPi PSX.ATT)
let CMD = (psxToPi PSX.CMD)
let CLK = (psxToPi PSX.CLK)
let DAT = (psxToPi PSX.DAT)

// this exists so printing to the console doesn't mess up the timing of the main thread
let printer = MailboxProcessor<string>.Start(fun inbox -> 
    let rec loop() = async {
        let! msg = inbox.Receive()
        printfn "%s" <| msg
        return! loop()
    }
    loop())

let printHex (x:byte) = printer.Post <| System.String.Format("{0:X2}", x)

type ConfigOptions = {
    Pressures : bool
    Analogue  : bool
    Rumble  : bool 
}

type PadData =
    | Digital     of data : byte array  
    | AnalogueRed of data : byte array
    | Config      of data : byte array
    
let (|IsDigital|_|)     b = if b = 0x41uy then Some () else None
let (|IsAnalogueRed|_|) b = if b = 0x73uy then Some () else None
let (|IsConfigMode|_|)  b = if b = 0xF3uy then Some () else None

let enterConfig =  [|0x01uy;0x43uy;0x00uy;0x01uy;0x00uy;0x00uy;0x00uy;0x00uy;0x00uy|]
let setMode =      [|0x01uy;0x44uy;0x00uy;0x01uy;0x03uy;0x00uy;0x00uy;0x00uy;0x00uy|]
let exitConfig =   [|0x01uy;0x43uy;0x00uy;0x00uy;0x5Auy;0x5Auy;0x5Auy;0x5Auy;0x5Auy|]
let enableRumble = [|0x01uy;0x4Duy;0x00uy;0x00uy;0x01uy|]

let sendCommand bytes delay =   
    write ATT false
    let data = [| for x in bytes -> spi x |]            
    write ATT true
    delayMs delay // wait a bit
    data

let readPad() = 
    let getData() =
        write ATT false     
        let data =
            [| 
                // prepare
                yield spi 0x1uy
                // get data
                let mode = spi 0x42uy 
                yield mode
                // 0x5A and two data bytes
                for x in 0..2 -> spi 0x0uy
                for x in 0..3 -> spi 0x0uy
//                match mode with
//                | IsAnalogueRed -> 
//                    // rest of analogue data
//                    for x in 0..3 -> spi 0x0uy
//                | _ -> ()
            |] 
        write ATT true
        data
    
    // try a a few times to get some data since its a bit erratic with timing and all
    let rec aux count = 
        if count > 0 then  
            let data = getData()
            match data with
            | [|_;IsDigital;0x5Auy;data1;data2;data3;data4;data5;data6|] -> 
//                printer.Post "digi"
                if data1 <> 0xFFuy || data2 <> 0xFFuy then 
                    printer.Post "something pressed"
//                printer.Post (System.String.Format("\t\t1 {0:X2}", data1))
//                printer.Post (System.String.Format("\n\t\t2 {0:X2}", data2))
                Some (Digital [|data1;data2|])
            | [|_;IsAnalogueRed;0x5Auy;data1;data2;data3;data4;data5;data6 |] as x -> 
                printer.Post "analog"
                Some (AnalogueRed x)
            | [|_;IsConfigMode;0x5Auy;data1;data2;data3;data4;data5;data6 |] as x -> 
                printer.Post "config mode"
                Some (Config x)
            | data -> 
              //  printer.Post (sprintf "something else %A" data.Length)
              //  for x in data do printer.Post (System.String.Format("{0:X2}", x))
              //  delayMs 5
                aux (count-1)
        else 
            None
    
    aux 1


let setupPad() =
    // we always want the pad to be in analog mode
    // first check if its talking at all
    match readPad() with
    | Some (Digital data)-> 
        let rec aux count = 
            if count > 0 then
                // attempt to switch to analog mode
                sendCommand enterConfig 0|> ignore
                sendCommand setMode     0|> ignore
                sendCommand enableRumble 0|> ignore
                sendCommand exitConfig  0|> ignore
                match readPad() with
                | Some(AnalogueRed  _)-> 
                    printer.Post "Configured pad sucessfully"
                    true
                | Some (Digital _) -> 
                    printer.Post "Still in digital mode :("
                    
                    aux (count-1)
                
                | _ -> aux (count-1)
            else 
                printer.Post "Failed to configure pad"
                false
        aux 10
        //true
    | Some(Config _)->
        printer.Post "Pad already in config mode"    
        sendCommand exitConfig  0|> ignore
        true
                
    | Some(AnalogueRed _)->
        printer.Post "Pad already in red mode"    
        true
    | None -> 
        //printer.Post "Pad not responding, could not configure"
        false
                