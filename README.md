HOLMS PBX Connector
===============

The HOLMS PBX Connector publishes output from a Mitel PBX to RabbitMQ, for use in a hotel property management system. 

We hope that by posting this, we can save you the hassle of decoding this prehistoric dinosaur's protocols. The protocols aren't that complex, but please copy liberally to save yourself the hassle of debugging yet another poorly documented, low-level protocol. 

The component acts in service of three business processes at the hotel:

    * Housekeeping signaling: when a housekeeper finishes cleaning a room, they signal completion by dialing a code on the room's phone.
    * Call accounting: the PBX signals each outward-dialed call, so that the hotel can record the call, and post the charge.
    * Phone enablement/disablement: the room's call line is enabled/disabled when a reservation starts/ends.

Tested with the following Mitel PBXes
-----------------------------------------
    * SX-50 Digital 
    * SX-100 (Analog)
    * SX-200 AX
    * SX-200MX (ICP)
    * SX-200MX ELML (no Ethernet)
    
The PBXes use two protocols: a line-based, textual protocol called Station Message Detail Recording (SMDR), which relays information about dialed calls, and a more sophisticated, binary "Property Management System Protocol" which permits bidirectional status and command signaling between the Mitel PBX and HOLMS. We support a subset of both protocols.

If you use this, please get in touch with us - hello@shortbar.com. We'd love to swap stories and hear what worked, what didn't, etc.
