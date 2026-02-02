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
				content.AppendLine(": the 'pool' from where the next song will be chosen", null);
				content.Append(" Library", Palette.info);
				content.AppendLine(": the whole collection of songs", null);
				content.Append(" Order", Palette.info);
				content.AppendLine(": the order in which the next song will be chosen: order, shuffle, smart shuffle", null);
				content.Append(" Queue", Palette.info);
				content.AppendLine(": if not empty, next song will be chosen from here instead of source. There is an additional option to not empty it (this allows for repetition)", null);
				break;
			case 1:
				content.AppendLine("Importing", Palette.info);
				
				content.AppendLine(" AshRadio uses ffmpeg to import non .mp3 files, transforming them. Therefore, you can import almost any audio format from files.", null);
				content.AppendLine(" To download from youtube and other websites, AshRadio uses yt-dlp. This program downloads audio files from multiple web pages, allowing for easier importing.", null);
				content.AppendLine(" Go to the config to change the paths of these executables or auto-download them.", null);
				break;
			case 2:
				content.AppendLine("Keybinds", Palette.info);
				
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
				content.AppendLine(" You can check the stats divided into months.", null);
				content.AppendLine(" Every time a song is loaded into the player, it counts as 'song laoded'.", null);
				content.AppendLine(" Then, the time listening to that song while it is playing is tracked.", null);
				content.AppendLine(" Also, dividing the time lime listened by the duration gives a much more accurate number of times the song was listened to. This is what is called 'song listened'.", null);
				content.AppendLine(" When seeing the stats you can filter between these numbers.", null);
				content.AppendLine(" Additionally, you can see top authors and their top tracks.", null);
				break;
			case 4:
				content.AppendLine("Exporting", Palette.info);
				content.AppendLine(" You can export songs or whole playlists to folders.", null);
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
	
	void setAbout(){
		if(currentMiddleScreen.identifier == "about"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		MiddleScreen l3 = generateMiddle(new TuiSelectable[,]{{
			new TuiButton("Open GitHub repo", Placement.BottomCenter, 0, 4, null, Palette.user).SetAction((s, ck) => {
				openUrl("https://github.com/siljamdev/AshRadio");
			})
		}});
		l3.identifier = "about";
		
		l3.screen.Elements.Add(new TuiLabel("About", Placement.TopCenter, 0, 1, Palette.main));
		l3.screen.Elements.Add(new TuiLabel("AshRadio v" + BuildInfo.Version, Placement.TopCenter, 0, 3));
		l3.screen.Elements.Add(new TuiLabel("Version built on: " + BuildInfo.BuildDate, Placement.TopCenter, 0, 4));
		l3.screen.Elements.Add(new TuiTwoLabels("Made by ", "siljam", Placement.TopCenter, 0, 6, null, Palette.author));
		l3.screen.Elements.Add(new TuiLabel("This software is under the MIT license", Placement.TopCenter, 0, 7, Palette.info));
		
		setMiddleScreen(l3);
	}
}