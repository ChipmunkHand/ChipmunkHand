[<AutoOpen>]
module Hardware

#nowarn "9" // unverifiable constructs
#nowarn "51" // The address-of operator may result in non-verifiable code. Its use is restricted to passing byrefs to functions that require them

// holds all the boring stuff to p/invoke the native code allowing access to chip functions
// also pin mappings from the pi to the other hardware (controller, etc).
open System
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop

type NativeArray<'T when 'T : unmanaged>(ptr : nativeptr<'T>, len: int) =
    member x.Ptr = ptr
    [<NoDynamicInvocation>]
    member inline x.Item 
       with get n = NativePtr.get x.Ptr n
       and  set n v = NativePtr.set x.Ptr n v
    member x.Length = len


type PinnedArray<'T when 'T : unmanaged>(narray: NativeArray<'T>, gch: GCHandle) =
    [<NoDynamicInvocation>]
    static member inline OfArray(arr: 'T[]) =
        let gch = GCHandle.Alloc(box arr,GCHandleType.Pinned)
        let ptr = &&arr.[0]
        new PinnedArray<'T>(new NativeArray<_>(ptr, Array.length arr), gch)

    member x.Ptr = narray.Ptr
    member x.Free() = gch.Free()
    member x.Length = uint32 narray.Length
    member x.NativeArray = narray
    interface System.IDisposable with 
        member x.Dispose() = gch.Free()

type GPIODirection =
    | In = 0
    | Out = 1

//The physical pin
type GPIOPins =
    | GPIO_None = 4294967295u
    | Pin_SDA = 2u
    | Pin_SCL = 3u
    | Pin_7   = 4u
    | Pin_11  = 17u
    | Pin_12  = 18u
    | Pin_13  = 27u
    | Pin_15  = 22u
    | Pin_16  = 23u
    | Pin_18  = 24u
    | Pin_22  = 25u

type PSX =
    | DAT  // Controller -> PSX (input)
    | CMD  // PSX -> Controller
    | ATT  // shout at controller
    | CLK

let psxToPi psx =
    match psx with
    | PSX.DAT -> GPIOPins.Pin_11
    | PSX.CMD -> GPIOPins.Pin_16
    | PSX.ATT -> GPIOPins.Pin_22
    | PSX.CLK -> GPIOPins.Pin_15

[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_init")>]
extern bool bcm2835_init()
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_gpio_fsel")>]
extern void bcm2835_gpio_fsel(GPIOPins pin, bool mode_out);
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_gpio_write")>]
extern void bcm2835_gpio_write(GPIOPins pin, bool value);
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_gpio_lev")>]
extern bool bcm2835_gpio_lev(GPIOPins pin);
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_delayMicroseconds")>]
extern void bcm2835_delayMicroseconds(uint64 micros);
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_st_read")>]
extern uint64 bcm2835_st_read();
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_spi_begin")>]
extern int bcm2835_spi_begin()
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_spi_end")>]
extern int bcm2835_spi_end()
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_spi_setClockDivider")>]
extern void bcm2835_spi_setClockDivider(uint16 divider)

[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_spi_setDataMode")>]
extern void bcm2835_spi_setDataMode(uint8 mode)

//Transfers one byte to and from the currently selected SPI slave. 
//Asserts the currently selected CS pins (as previously set by bcm2835_spi_chipSelect) 
// during the transfer. Clocks the 8 bit value out on MOSI, and simultaneously clocks 
//in data from MISO. Returns the read data byte from the slave. Uses polled transfer 
//as per section 10.6.1 of the BCM 2835 ARM Peripherls manual
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_spi_transfer")>]
extern uint8 bcm2835_spi_transfer(uint8 value)

(*Transfers any number of bytes to and from the currently selected SPI slave using bcm2835_spi_transfernb. 
The returned data from the slave replaces the transmitted data in the buffer.  Parameters:
[in,out] buffer: Buffer of bytes to send. Received bytes will replace the contents
[in] len: Number of bytes in the buffer, and the number of bytes to send/received *)
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_spi_transfern")>]
extern unit bcm2835_spi_transfern(byte* buffer, uint32 lenght)



// friendly wrappers 
let fsel pin value = bcm2835_gpio_fsel(pin,value)                        
let write pin value = bcm2835_gpio_write(pin,value)            
let read pin = bcm2835_gpio_lev(pin)
let delayMs (ms:int) = System.Threading.Thread.Sleep ms
let delayUs us = bcm2835_delayMicroseconds us
let spi b = bcm2835_spi_transfer (revByte b) |> byte |> revByte
let spiBytes xs = 
        let reverseXs = xs|> Array.map(revByte) 
        let pa = PinnedArray.OfArray(reverseXs)
        bcm2835_spi_transfern(pa.Ptr, pa.Length)
    