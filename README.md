# HNS Name Alarm

Only Works on Windows

To use it just run bob and paste in your API key into the app. When it sees bob is open it will get the names and will save them in file in Appdata/local (with the expiry blocks.
Once synced it can work without bob runnning and will sync any updates upon opening bob
Then it will pull the current block from https://api.handshakeapi.com/hsd and will send an notification if the name will expire in the next 2000 blocks (~2 weeks) can be set by changing the settings.txt in the appdata folder

All settings are in AppData/Loacal/HNSAlarm

To run either build in Visual studio or run setup.exe from the zip file.

You can make it run on computer login/startup by:
1. Search for HNSNameAlarm and click `Open Location`
2. Open Run (CTRL+R) and use `shell:startup`
3. Add a shortcut to the HNSNameALarm from the shell:startup window
4. It will now start HNS Alarm on login

![image](https://user-images.githubusercontent.com/62039630/171986180-cd942d62-81eb-48a1-9fd3-864e5a230047.png)
![image](https://user-images.githubusercontent.com/62039630/171986182-3685a11b-8f1e-4791-82b1-8a1e15391685.png)
