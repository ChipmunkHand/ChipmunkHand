Introduction

This post and work herein is by both Andrea and Ross, but will be written in an odd first/third person style. This post is part of the F# advent calendar 2016.

In our current super-secret yet not-very-secret but as-of-yet-mostly-not-announced hardware project, we have a requirement to use a controller.  We are currently using a Raspberry Pi 2 and would like to write most of the software, for the time being, in F#.  Now, Ross' blog already has some details on using a NES pad <link>, but for this project we are going to need way more buttons and analogue sticks, to this end, we settled on the wireless Playstation 2 Controller (henceforth known as PSX)

Detectives

Sony never officially released the protocol specifications for their controller (of course not, that would be too easy). A quick search around the internet will yield various incomplete, conflicting documentation on how it works.  The top level concepts can be loosely explained as follows.

1) There is a master / slave relationship with the Playstation (Server) obviously being the master
2) The clock signal used to synchronise the two systems runs at a frighteningly fast pace,somehwere between 250khz-500khz
3) Data is full duplex, this means the master and slave both send data to each other on the same clock cycle on two different lines
4) The controller has the capability to enter a configuration mode where you can tell it switch stuff on and off, such as the analogue sticks, the button pressure sensors and the rumble motors.

Yakception

Let's talk about speed quickly (see what we did there).  The clock cycle to keep the devices in sync is somewhere between 250 to 500 kilohertz.  1 hertz is one cycle per second, this means a rate of 500 kilohertz is equal to about 1 cycle every 4 microseconds(!).  Unfortunately being in a managed language on top of an operating system makes this rather difficult, the CLR only lets us delay at 1 millisecond at most, and even that is not guranteed due to the operating system scheduler.  Thankfully, using the pi we have access to a bunch of functions on the chip including a microsecond delay function, which is also not guranteed to do you what tell it to (that would also be too easy) and you might be lucky to get a delay lower than 80 microseconds.

Well, let's give it a go anyway. We are using a Lynxmotion wireless PSX controller, here is a schematic on how we hooked up the Pi to the PSX.

<pic>

The general format of communications goes as follows

1) Server pulls the ATT (Attention!) line LOW.  This tells the controller that the serveris about to initialize communications with it
2) Server begins the clock cycle by pulling LOW for 4us then HIGH for 4us and so on
3) As the clock goes LOW, each device loads its next bit onto the relevant line.  The server sends data on the CMD line, whilst the slave sends data on the DAT line.  When the clock line goes HIGH, each side reads the bit they were sent, and the process continues
4) The Server pulls the ATT line HIGH again once it has finished.

Now, since we have to send data out bit-by-bit we are going to need to write some functions to convert a byte to and from a sequence of HIGH/LOW (0/1, true/false) values.

[code here]

It seems the most simple thing you can do is to ask the controller for the state of its digital buttons.  Not forgetting the full-duplex communication, the whole series of communications happens over 5 bytes as follows.

byte     | master | slave |
1        | 0x1    | 0xFF  | 
2        | 0x42   | 0x41  |
3        | 0x0    | 0x5A  |
4        | 0x0    | 0xFF  |
4        | 0x0    | 0xFF  |

In the first byte we ignore what the slave says, we are just initiaing the commnunication, which seems to always be with 0x1.  The second byte tells the controller what to do, 0x42 tells it to send the states of the digital buttons.  At the same time, the controller sends us 0x41. This byte might be something else, and it is the controller telling us what sort of mode it is in (eg, digital, angalogue).

For the remaining bytes, the master sends nothing at all. The slave sends one byte 0x5A which is it confirming it is about to send the data, and finally it sends two bytes that represent the state of the 16 digital buttons, on per bit,  with LOW being pressed.

(note: the controller expects these bytes to be send least-signifcant-bit first. Our code above that converts the bytes to a bool list handles this simply by not reversing the output list)

(note of a note: who cares if it is least signifant byte first? how would you even know when reverse engineering the protocol? you could equally call 0x41 0x82 and have done with it)
Here is an example program to try this (note the original one was way messier than this)

[code]

Did this work? Of course it didn't.  Occasionally we could get a message saying it had detected the 0x41 byte indicating the controler had told us it was in digital mode (woohoo!) but nothing other than that.  Something to be careful of here is writing to the console takes a very long time and messes the timing up, thas was taken into consideration too (not shown here).

Looking at what was going on with the oscilliscope and logic analyzer, we could see the clock cycle was all over the place - we expected that would be the case anyway, but was not sure how it would affect the controller.  Does the controller time out and reset itself? Were we doing something completely wrong?

Further adventures in Yak land

Ah, behold the familiar fields of Yaks waiting to be shaved!
