# YAIBA-VRC
YAIBA library for VRChat World

## Features
* Staff setting feature
    * Register a specific player as a staff member
    * Output position, rotation and questionnaire infomation to VRChat log files only for players registered as staff
    * Set GameObjects to be displayed only to players registered as staff
* Position and rotation logging feature
    * Confirm the player's consent for position and rotation logging
    * Periodically obtain and log position and rotation information for players who agree to.
    * Display the position and rotation information of players who agree to on the in-game debug panel (for debugging)
    * The maximum number of total players(e.g. maximum PlayerId) is 500. Logging of players exceeding this number will not be performed.
* Questionnaire feature
    * Set up questionnaire question text and choices
    * Obtain and log questionnaire results

## Usage
1. Import VRCSDK3-WORLD and UdonSharp
    * Confirmed in the following versions
        * VRCSDK3-WORLD-2022.06.03.00.03_Public + UdonSharp_v0.20.3
        * VRChat SDK Base 3.1.7 + VRChat SDK World 3.1.7 + UdonSharp 1.1.1
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
        * YAIBA/Questionnaire/QuestionnaireGimmick
            * Questions：Set up the Question Text and Choices; **the Size of Questions must be less than or equal to 11.**
            Each Element consists of a question in the first paragraph, followed by Choices(up to 6 Choices). **Size of each Element should be 7 or less.**
            * Questions Fontsize：Font size of the question text and choices.
            * Next Question：Next question number for each choice. The question number for Question 1 is [0], Question 2 is [1]...  If the question number is set to [Size of Questions], it will be redirected to the questionnaire confirmation screen.
5. If **Yodokoro Tag Marker** is placed in a scene, [YAIBA](https://github.com/ScienceAssembly/YAIBA) can analyze its logs to collect tag information.  
Learn more about **Yodokoro Tag Marker** here, [生チョコ教団 ヨドコロちゃんのタグマーカー](https://booth.pm/ja/items/3109716)

## License
YAIBA-VRC is under MIT license.  
YAIBA-VRC is copyrighted by the VRC Science Assembly(VRC理系集会).  
It would be nice if you could indicate in the VRChat world that you are using YAIBA library.