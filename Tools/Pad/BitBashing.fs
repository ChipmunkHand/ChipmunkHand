[<AutoOpen>]
module BitBashing

let convertToBits n =
    let mutable mask = 0b00000001
    [for i in 0 .. 7 do
        let r =  n &&& mask
        mask <- mask <<< 1
        yield (if r > 0 then 1uy else 0uy)]
    |> List.rev

// converts a list of 8 bits (a byte is the smallest we can go) to a byte 
let convertToNumber (xs: byte list) =
    let folder (acc, counter) x  =
        let bitShiftedValue = (x <<< (int counter)) 
        (acc ||| bitShiftedValue, counter + 1uy)    
    List.fold folder (0uy, 0uy) xs |> fst     

let revByte =
    let rev (input:byte) =
        let mutable output = 0uy
        // no loop implementation of reverse, for no good reason at all
        output <- output ||| ((input &&& (1uy<<<0)) <<< 7)
        output <- output ||| ((input &&& (1uy<<<7)) >>> 7)        
        output <- output ||| ((input &&& (1uy<<<1)) <<< 5)
        output <- output ||| ((input &&& (1uy<<<6)) >>> 5)
        output <- output ||| ((input &&& (1uy<<<2)) <<< 3)
        output <- output ||| ((input &&& (1uy<<<5)) >>> 3)
        output <- output ||| ((input &&& (1uy<<<3)) <<< 1)
        output <- output ||| ((input &&& (1uy<<<4)) >>> 1)
        output

    //store reverse bytes as lookups since it costs just 512 bytes (plus dict) and give us O(1)
    let lookup = [for x in 0uy..255uy -> x, rev x] |> dict
    fun b -> lookup.[b]
