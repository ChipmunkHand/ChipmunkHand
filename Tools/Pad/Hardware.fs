[<AutoOpen>]
module Hardware

// holds all the boring stuff to p/invoke the native code allowing access to chip functions
// also pin mappings from the pi to the other hardware (controller, etc).

open System.Runtime.InteropServices

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
    | PSX.ATT -> GPIOPins.Pin_13
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
//Transfers one byte to and from the currently selected SPI slave. 
//Asserts the currently selected CS pins (as previously set by bcm2835_spi_chipSelect) 
// during the transfer. Clocks the 8 bit value out on MOSI, and simultaneously clocks 
//in data from MISO. Returns the read data byte from the slave. Uses polled transfer 
//as per section 10.6.1 of the BCM 2835 ARM Peripherls manual
[<DllImport("libbcm2835.so", EntryPoint = "bcm2835_spi_transfer")>]
extern uint8 bcm2835_spi_transfer(uint8 value)



// friendly wrappers 
let fsel pin value = bcm2835_gpio_fsel(pin,value)                        
let write pin value = bcm2835_gpio_write(pin,value)            
let read pin = bcm2835_gpio_lev(pin)
let delayMs (ms:int) = System.Threading.Thread.Sleep ms
let delayUs us = bcm2835_delayMicroseconds us
let spi b = bcm2835_spi_transfer (revByte b) |> byte |> revByte