# AshRadio
<img src="res/icon.png" width="200"/>

AshRadio is a lightweight music player that runs in the terminal.  
**The application will only work in an interactive console**

## Usage
You can import **songs** either from files natively or, using [yt-dlp](https://github.com/yt-dlp/yt-dlp), from youtube(or other pages).  
These songs can have multiple Authors and can be in playlists. Albums do not exist for simplicity, but playlists can easily do its job.  
The session panel contains the queue for customizing the songs playing and the source, a 'pool' of songs to choose next from.  
There is a very complete in-app Help menu.  

## Installation
You can install AshRadio for windows x64 or x86 with the portable executable.  
Compatibility with Linux or Mac in unlikely, because the audio library used only supports windows.

## License
This software is licensed under the [MIT License](https://github.com/siljamdev/AshRadio/blob/main/LICENSE).

## Internal operation
It is powered by [AshConsoleGraphics](https://github.com/siljamdev/AshConsoleGraphics). This library, made by myself, allows to easily make console applications as rich as this one.