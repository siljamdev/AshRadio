# AshRadio
<img src="res/icon.png" width="200"/>

AshRadio is a lightweight music player that runs in the terminal

## Usage
You can import **songs** either from files natively or, using [yt-dlp](https://github.com/yt-dlp/yt-dlp), from youtube(or other pages).  
These songs can have multiple Authors and can be in playlists. Albums do not exist for simplicity, but playlists can easily do its job.  
The session panel contains the queue for customizing the songs playing and the source, a 'pool' of songs to choose next from.  
There is a very complete in-app Help menu.  

**The application will only work in an interactive console**  

## Installation
You can install AshRadio for Windows, Linux and Mac with the portable executable from the [releases](https://github.com/siljamdev/AshRadio/releases/latest).  
Compatibility with Linux or Mac is **untested**.

## CLI
This application features a useful CLI for basic automatable actions, and has easy to parse output.  
To see more about it, do `ashradio -h`

## License
This software is licensed under the [MIT License](./LICENSE).

## Internal operation
It is powered by [AshConsoleGraphics](https://github.com/siljamdev/AshConsoleGraphics). This library, made by myself, allows to easily make console applications as rich as this one.  
It also uses [Managed BASS](https://github.com/ManagedBass/ManagedBass) library for audio.