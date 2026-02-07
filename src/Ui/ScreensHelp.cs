using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{	
	void setHelp(int page = 0, bool ignore = false){
		if(!ignore && currentMiddleScreen.identifier == "help"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		const int maxPage = 5;
		
		if(page < 0 || page > maxPage){
			return;
		}
		
		MiddleScreen l3 = generateMiddle(null);
		l3.identifier = "help";
		
		//Juto to avoid rewriting a lot of code
		TuiScreenInteractive l = l3.interactive;
		
		TuiFormatLog content = new TuiFormatLog(l.Xsize - 4, l.Ysize - 7, Placement.TopLeft, 2, 5);
		
		content.OnParentResize += (s, a) => {
			content.Xsize = l.Xsize - 4;
			content.Ysize = l.Ysize - 7;
			
			content.ScrollToTop();
		};
		
		l.Elements.Add(content);
		
		l.Elements.Add(new TuiLabel("Help - Page " + (page + 1), Placement.TopCenter, 0, 1, Palette.main));
		
		//Helper method
		void setPage(int n){
			if(n != page && n > -1 && n <= maxPage){
				setHelp(n, true);
				removeMiddleScreen(l3);
			}
		}
		
		if(page > 0){
			Keybinds.listUp.subEvent(l3, "Previous page", (s, ck) => {
				setPage(page - 1);
			});
			
			Keybinds.left.subEvent(l3, false, (s, ck) => {
				setPage(page - 1);
			});
		}
		
		if(page < maxPage){
			Keybinds.listDown.subEvent(l3, "Next page", (s, ck) => {
				setPage(page + 1);
			});
			
			Keybinds.right.subEvent(l3, false, (s, ck) => {
				setPage(page + 1);
			});
		}
		
		for(int i = 0; i <= maxPage; i++){
			int j = i;
			Keybinds.getNumber(i + 1).subEvent(l3, false, (s, ck) => {
				setPage(j);
			});
		}
		
		Keybinds.scrollUp.subEvent(l3, true, (s, ck) => {
			content.Scroll++;
		});
		
		Keybinds.scrollDown.subEvent(l3, true, (s, ck) => {
			content.Scroll--;
		});
		
		switch(page){
			case 0:
				content.AppendLine("Concepts", Palette.info);
				
				content.Append(" Session", Palette.info);
				content.AppendLine(": the current options for source, order and queue", null);
				content.Append(" Source", Palette.info);
				content.AppendLine(": the 'pool' where the next song will be chosen from", null);
				content.Append(" Order", Palette.info);
				content.AppendLine(": the order in which the next song will be chosen: order, shuffle, smart shuffle", null);
				content.Append(" Queue", Palette.info);
				content.AppendLine(": if not empty, next song will be chosen from here instead of source. There is an additional option to not empty it (this allows for repetition)", null);
				content.Append(" Library", Palette.info);
				content.AppendLine(": the whole collection of songs", null);
				break;
			case 1:
				content.AppendLine("Importing", Palette.info);
				
				content.AppendLine(" AshRadio uses ffmpeg to import audio files, transforming them if needed. Therefore, you can import almost any audio format from files.", null);
				content.AppendLine(" To download from youtube and other websites, AshRadio uses yt-dlp. This program downloads audio files from multiple web pages, allowing for easier importing.", null);
				content.AppendLine(" Go to the config to change the paths of these executables or auto-download them.", null);
				break;
			case 2:
				content.AppendLine("Keybinds", Palette.info);
				content.AppendLine(" The Keybinds shown here are the ones configured, not the default ones. To change them, go to Config>Keybinds");
				
				content.AppendLine(" Basic movement:", null);
				foreach(Keybind k in new Keybind[]{Keybinds.up, Keybinds.down, Keybinds.left, Keybinds.right, Keybinds.scrollUp, Keybinds.scrollDown}){
					if(k.primary != null){
						content.Append("  " + k.ToString(), Palette.hint);
						content.AppendLine(" " + k.description, null);
						if(k.secondary != null){
							content.Append("  " + Keybind.keybindToString(k.secondary), Palette.hint);
							content.AppendLine(" " + k.description, null);
						}
					}
				}
				content.AppendLine("", null);
				
				content.AppendLine(" Wherever songs appear:", null);
				foreach(Keybind k in new Keybind[]{Keybinds.addToQueue, Keybinds.play, Keybinds.addToPlaylist, Keybinds.export, Keybinds.listRemove, Keybinds.listUp, Keybinds.listDown}){
					if(k.primary != null){
						content.Append("  " + k.ToString(), Palette.hint);
						content.AppendLine(" " + k.description, null);
						if(k.secondary != null){
							content.Append("  " + Keybind.keybindToString(k.secondary), Palette.hint);
							content.AppendLine(" " + k.description, null);
						}
					}
				}
				content.AppendLine("", null);
				
				content.AppendLine(" Authors & playlists:", null);
				foreach(Keybind k in new Keybind[]{Keybinds.setSource, Keybinds.search, Keybinds.export}){
					if(k.primary != null){
						content.Append("  " + k.ToString(), Palette.hint);
						content.AppendLine(" " + k.description, null);
						if(k.secondary != null){
							content.Append("  " + Keybind.keybindToString(k.secondary), Palette.hint);
							content.AppendLine(" " + k.description, null);
						}
					}
				}
				content.AppendLine("", null);
				
				content.AppendLine(" Volume:", null);
				foreach(Keybind k in new Keybind[]{Keybinds.volumeUp, Keybinds.volumeDown, Keybinds.volumeMute, Keybinds.volumeMax}){
					if(k.primary != null){
						content.Append("  " + k.ToString(), Palette.hint);
						content.AppendLine(" " + k.description, null);
						if(k.secondary != null){
							content.Append("  " + Keybind.keybindToString(k.secondary), Palette.hint);
							content.AppendLine(" " + k.description, null);
						}
					}
				}
				content.AppendLine("", null);
				
				content.AppendLine(" Player:", null);
				foreach(Keybind k in new Keybind[]{Keybinds.pause, Keybinds.previous, Keybinds.skip, Keybinds.restart, Keybinds.rewind, Keybinds.advance, Keybinds.seePlaying}){
					if(k.primary != null){
						content.Append("  " + k.ToString(), Palette.hint);
						content.AppendLine(" " + k.description, null);
						if(k.secondary != null){
							content.Append("  " + Keybind.keybindToString(k.secondary), Palette.hint);
							content.AppendLine(" " + k.description, null);
						}
					}
				}
				content.AppendLine("", null);
				
				content.AppendLine(" Navigation:", null);
				foreach(Keybind k in new Keybind[]{Keybinds.library, Keybinds.authors, Keybinds.playlists, Keybinds.import, Keybinds.stats, Keybinds.selectQueue, Keybinds.help, Keybinds.config}){
					if(k.primary != null){
						content.Append("  " + k.ToString(), Palette.hint);
						content.AppendLine(" " + k.description, null);
						if(k.secondary != null){
							content.Append("  " + Keybind.keybindToString(k.secondary), Palette.hint);
							content.AppendLine(" " + k.description, null);
						}
					}
				}
				content.AppendLine("", null);
				
				content.AppendLine(" Session:", null);
				foreach(Keybind k in new Keybind[]{Keybinds.changeMode, Keybinds.seeSource, Keybinds.toggleQueueEmpties, Keybinds.changeDevice}){
					if(k.primary != null){
						content.Append("  " + k.ToString(), Palette.hint);
						content.AppendLine(" " + k.description, null);
						if(k.secondary != null){
							content.Append("  " + Keybind.keybindToString(k.secondary), Palette.hint);
							content.AppendLine(" " + k.description, null);
						}
					}
				}
				break;
			case 3:
				content.AppendLine("Stats", Palette.info);
				content.AppendLine(" You can check your listening statistics divided into months.", null);
				content.AppendLine(" Every time a song is loaded into the player, it counts as 'song laoded'.", null);
				content.AppendLine(" Then, the time listening to that song while it is playing is tracked.", null);
				content.AppendLine(" Also, dividing the time time listened by the duration gives a much more accurate number of times the song was listened to. This is what is called 'song listened'.", null);
				content.AppendLine(" When seeing the stats you can filter between these numbers.", null);
				content.AppendLine(" Additionally, you can see top authors and their top tracks.", null);
				break;
			case 4:
				content.AppendLine("Exporting", Palette.info);
				content.AppendLine(" You can export songs, whole playlists and all songs by an author to folders.", null);
				content.AppendLine(" This allows you to have the mp3 files of your songs wherever you want, or share them.", null);
				break;
			case 5:
				content.AppendLine("Internal operation", Palette.info);
				
				content.AppendLine(" AshRadio uses numerical ids for songs, authors and playlists.", null);
				content.AppendLine(" 2147483647 is the maximum id. Try not importing that many songs!", null);
				content.AppendLine(" For the audio playing, ManagedBass is used. This .net wrapper of the c BASS library makes it really easy to play audio files.", null);
				content.AppendLine(" For data storage and many other tasks, AshLib is used. This .net library (made by me!) handles AshFiles.", null);
				content.AppendLine(" The UI in the console is made using AshConsoleGraphics, a .net library also made by me.", null);
				break;
		}
		content.ScrollToTop();
		
		setMiddleScreen(l3);
	}
}