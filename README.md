# YAIBA-VRC
YAIBA library for VRChat World

## Features
* Staff setting feature
    * Register a specific player as a staff member
    * Output position and rotation infomation to VRChat log files only for players registered as staff
    * Set GameObjects to be displayed only to players registered as staff
* Position and rotation logging feature
    * Confirm the player's consent for position and rotation logging
    * Periodically obtain and log position and rotation information for players who agree to.
    * Display the position and rotation information of players who agree to on the in-game debug panel (for debugging)

## Usage
1. Import VRCSDK3-WORLD and UdonSharp
    * Confirmed in the following versions
        * VRCSDK3-WORLD-2022.06.03.00.03_Public.unitypackage + UdonSharp_v0.20.3.unitypackage
2. Import the YAIBA Package
3. Place VRCWorld Prefab in Scene
4. Place YAIBA Prefab in Scene and adjust its position
    * Main parameters of YAIBA Prefab
        * YAIBA/StaffSetting
            * Staff Names：Set the name(s) of the player(s) to be registered as staff
            * Staff Active Objects：Set object(s) to be visible only to staff. **Do not deactivate the default "DebugSwitch".**
        * YAIBA/Orientation/OrientationGimmick
            * Measure Interval：Interval at which position and rotation information log to a VRChat log file
            * Teleport to：Teleport destination after confirming the player's consent to the position and rotation logging. If teleportation is not required, it is deactivated.

## License
YAIBA-VRC is under MIT license.