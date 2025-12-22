using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Time;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{
	void setConfig(){
		TuiSelectable[,] t = new TuiSelectable[,]{{
			new TuiButton("Palette", Placement.TopCenter, 0, 4, null, Palette.user).SetAction((s, ck) => {
				setPaletteConfig();
			})
		},{
			new TuiButton("Player", Placement.TopCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
				setPlayerConfig();
			})
		},{
			new TuiButton("Paths", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				setPathConfig();
			})
		},{
			new TuiButton("Miscellaneous", Placement.TopCenter, 0, 10, null, Palette.user).SetAction((s, ck) => {
				setMiscConfig();
			})
		}};
		
		MiddleScreen l = generateMiddle(t);
		
		l.interactive.Elements.Add(new TuiLabel("Config", Placement.TopCenter, 0, 1, Palette.main));
		l.interactive.Elements.Add(new TuiTwoLabels("AshRadio v" + Radio.version, " made by siljam", Placement.BottomRight, 0, 0, Palette.hint, null));
		
		setMiddleScreen(l);
	}
	
	void setPaletteConfig(){
		TuiFramedCheckBox useCols = new TuiFramedCheckBox(' ', 'X', Radio.config.GetValue<bool>("ui.useColors"), Placement.TopCenter, 4, 4, null, null, null, Palette.user, Palette.user);
		
		TuiFramedTextBox user = setColor3(new TuiFramedTextBox(Palette.user?.foreground.ToString() ?? "", 7, Placement.TopLeft, 8, 7, null, Palette.user, Palette.user));
		TuiFramedTextBox main = setColor3(new TuiFramedTextBox(Palette.main?.foreground.ToString() ?? "", 7, Placement.TopCenter, 2, 7, null, Palette.main, Palette.user));
		TuiFramedTextBox background = setColor3(new TuiFramedTextBox(Palette.background?.background.ToString() ?? "", 7, Placement.TopRight, 1, 7, null, null, Palette.user));
		
		TuiFramedTextBox song = setColor3(new TuiFramedTextBox(Palette.song?.foreground.ToString() ?? "", 7, Placement.TopLeft, 8, 10, null, Palette.song, Palette.user));
		TuiFramedTextBox author = setColor3(new TuiFramedTextBox(Palette.author?.foreground.ToString() ?? "", 7, Placement.TopCenter, 2, 10, null, Palette.author, Palette.user));
		TuiFramedTextBox playlist = setColor3(new TuiFramedTextBox(Palette.playlist?.foreground.ToString() ?? "", 7, Placement.TopRight, 1, 10, null, Palette.playlist, Palette.user));
		
		TuiFramedTextBox hint = setColor3(new TuiFramedTextBox(Palette.hint?.foreground.ToString() ?? "", 7, Placement.TopLeft, 8, 13, null, Palette.hint, Palette.user));
		TuiFramedTextBox info = setColor3(new TuiFramedTextBox(Palette.info?.foreground.ToString() ?? "", 7, Placement.TopCenter, 2, 13, null, Palette.info, Palette.user));
		TuiFramedTextBox delimiter = setColor3(new TuiFramedTextBox(Palette.delimiter?.foreground.ToString() ?? "", 7, Placement.TopRight, 1, 13, null, Palette.delimiter, Palette.user));
		
		TuiLabel errorLabel = new TuiLabel("", Placement.BottomCenter, 0, 4, Palette.error);
		
		TuiButton setAshTheme = new TuiButton("Ash Theme", Placement.BottomLeft, 3, 5, null, Palette.user).SetAction((s, ck) => {
			Palette.setAsh();
			closeMiddleScreen(); //update
			setPaletteConfig();
		});
		
		TuiButton setSubtleTheme = new TuiButton("Subtle Theme", Placement.BottomCenter, 0, 5, null, Palette.user).SetAction((s, ck) => {
			Palette.setSubtle();
			closeMiddleScreen(); //update
			setPaletteConfig();
		});
		
		TuiButton setNeonTheme = new TuiButton("Neon Theme", Placement.BottomRight, 3, 5, null, Palette.user).SetAction((s, ck) => {
			Palette.setNeon();
			closeMiddleScreen(); //update
			setPaletteConfig();
		});
		
		bool save(){
			if(!Color3.TryParse(user.Text, out Color3 cUser)){
				errorLabel.Text = "Invalid user color. Try again";
				return false;
			}
			if(!Color3.TryParse(main.Text, out Color3 cMain)){
				errorLabel.Text = "Invalid main color. Try again";
				return false;
			}
			if(!Color3.TryParse(background.Text, out Color3 cBackground)){
				errorLabel.Text = "Invalid background color. Try again";
				return false;
			}
			if(!Color3.TryParse(song.Text, out Color3 cSong)){
				errorLabel.Text = "Invalid song color. Try again";
				return false;
			}
			if(!Color3.TryParse(author.Text, out Color3 cAuthor)){
				errorLabel.Text = "Invalid author color. Try again";
				return false;
			}
			if(!Color3.TryParse(playlist.Text, out Color3 cPlaylist)){
				errorLabel.Text = "Invalid playlist color. Try again";
				return false;
			}
			if(!Color3.TryParse(hint.Text, out Color3 cHint)){
				errorLabel.Text = "Invalid hint color. Try again";
				return false;
			}
			if(!Color3.TryParse(info.Text, out Color3 cInfo)){
				errorLabel.Text = "Invalid info color. Try again";
				return false;
			}
			if(!Color3.TryParse(delimiter.Text, out Color3 cDelimiter)){
				errorLabel.Text = "Invalid delimiter color. Try again";
				return false;
			}
			
			Radio.config.Set("ui.palette.user", cUser);
			Radio.config.Set("ui.palette.song", cSong);
			Radio.config.Set("ui.palette.author", cAuthor);
			Radio.config.Set("ui.palette.playlist", cPlaylist);
			Radio.config.Set("ui.palette.main", cMain);
			Radio.config.Set("ui.palette.delimiter", cDelimiter);
			Radio.config.Set("ui.palette.hint", cHint);
			Radio.config.Set("ui.palette.info", cInfo);
			Radio.config.Set("ui.palette.background", cBackground);
			
			Radio.config.Set("ui.useColors", useCols.Checked);
			
			errorLabel.Text = "";
			
			Radio.config.Save();
			
			Palette.init();
			
			return true;
		}
		
		TuiButton done = new TuiButton("Done", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			if(save()){
				closeMiddleScreen(); //update
				setPaletteConfig();
			}
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			useCols, useCols, useCols
		},{
			user, main, background
		},{
			song, author, playlist
		},{
			hint, info, delimiter
		},{
			setAshTheme, setSubtleTheme, setNeonTheme
		},{
			done, done, done
		}};
		
		TuiScreenInteractive l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Palette config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Use colors:", Placement.TopCenter, -4, 5));
		
		l.Elements.Add(new TuiLabel("User:", Placement.TopLeft, 2, 8));
		l.Elements.Add(new TuiLabel("Main:", Placement.TopCenter, -6, 8));
		l.Elements.Add(new TuiLabel("Background:", Placement.TopRight, 11, 8));
		
		l.Elements.Add(new TuiLabel("Song:", Placement.TopLeft, 2, 11));
		l.Elements.Add(new TuiLabel("Author:", Placement.TopCenter, -7, 11));
		l.Elements.Add(new TuiLabel("Playlist:", Placement.TopRight, 11, 11));
		
		l.Elements.Add(new TuiLabel("Hint:", Placement.TopLeft, 2, 14));
		l.Elements.Add(new TuiLabel("Info:", Placement.TopCenter, -6, 14));
		l.Elements.Add(new TuiLabel("Delimiter:", Placement.TopRight, 11, 14));
		
		l.Elements.Add(errorLabel);
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setPlayerConfig(){
		TuiFramedTextBox volumeExponent = setUFloat(new TuiFramedTextBox(Radio.py.volumeExponent.ToString(), 8, Placement.TopCenter, 8, 4, null, null, null, Palette.user, Palette.user));
		TuiFramedTextBox advanceTime = setUFloat(new TuiFramedTextBox(Radio.config.GetValue<float>("player.advanceTime").ToString(), 8, Placement.TopCenter, 8, 7, null, null, null, Palette.user, Palette.user));
		
		TuiLabel errorLabel = new TuiLabel("", Placement.BottomCenter, 0, 4, Palette.error);
		
		TuiButton reset = new TuiButton("Reset", Placement.BottomCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
			Radio.py.volumeExponent = 2f;
			Radio.config.Set("player.advanceTime", 5f);
			Radio.config.Save();
			
			closeMiddleScreen(); //update
			setPlayerConfig();
		});
		
		bool save(){
			if(!float.TryParse(volumeExponent.Text, out float f1)){
				errorLabel.Text = "Invalid volume exponent. Try again";
				return false;
			}
			if(!float.TryParse(advanceTime.Text, out float f2)){
				errorLabel.Text = "Invalid advance time. Try again";
				return false;
			}
			
			Radio.py.volumeExponent = f1;
			Radio.config.Set("player.advanceTime", f2);
			
			Radio.config.Save();
			
			errorLabel.Text = "";			
			return true;
		}
		
		TuiButton done = new TuiButton("Done", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			save();
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			volumeExponent
		},{
			advanceTime
		},{
			reset
		},{
			done
		}};
		
		TuiScreenInteractive l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Player config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Volume correction exponent:", Placement.TopCenter, -12, 5));
		l.Elements.Add(new TuiLabel("Advance time:", Placement.TopCenter, -5, 8));
		
		l.Elements.Add(errorLabel);
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setPathConfig(){
		TuiFramedScrollingTextBox ffmpeg = new TuiFramedScrollingTextBox(Radio.config.GetValue<string>("ffmpegPath"), 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox ytdlp = new TuiFramedScrollingTextBox(Radio.config.GetValue<string>("ytdlpPath"), 256, 16, Placement.TopCenter, 0, 10, null, null, null, Palette.user, Palette.user);
		
		ffmpeg.OnParentResize += (s, a) => {
			ffmpeg.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		ytdlp.OnParentResize += (s, a) => {
			ytdlp.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		TuiButton reset = new TuiButton("Reset", Placement.BottomCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
			Radio.config.Set("ffmpegPath", "ffmpeg");
			Radio.config.Set("ytdlpPath", "yt-dlp");
			Radio.config.Save();
			
			closeMiddleScreen(); //update
			setPathConfig();
		});
		
		TuiButton open1 = new TuiButton("Open ffmpeg.org", Placement.BottomLeft, 3, 3, null, Palette.user).SetAction((s, ck) => {
			openUrl("https://ffmpeg.org/");
		});
		
		TuiButton open2 = new TuiButton("Open yt-dlp downloads", Placement.BottomRight, 3, 3, null, Palette.user).SetAction((s, ck) => {
			openUrl("https://github.com/yt-dlp/yt-dlp/releases");
		});
		
		TuiButton auto = new TuiButton("Auto download all", Placement.BottomCenter, 0, 5, null, Palette.user).SetAction((s, ck) => {
			Radio.downloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
			Radio.dep.path + "/yt-dlp.exe", async () => {
				ytdlp.Text = Radio.dep.path + "/yt-dlp.exe";
				Radio.config.Set("ytdlpPath", removeQuotesSingle(ytdlp.Text));
				Radio.config.Save();
			});
			
			Radio.downloadFile("https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-7.1.1-essentials_build.zip",
			Radio.dep.path + "/temp.zip", async () => {
				try{
					ZipFile.ExtractToDirectory(Radio.dep.path + "/temp.zip", Radio.dep.path + "/temp", true);
					
					string p = Directory.GetFiles(Radio.dep.path + "/temp", "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
					
					if(File.Exists(Radio.dep.path + "/ffmpeg.exe")){
						File.Delete(Radio.dep.path + "/ffmpeg.exe");
					}
					
					File.Copy(p, Radio.dep.path + "/ffmpeg.exe");
					
					Directory.Delete(Radio.dep.path + "/temp", true);
					File.Delete(Radio.dep.path + "/temp.zip");
				}catch(Exception e){
					File.AppendAllText(Radio.errorFilePath, e.ToString() + "\n");
				}
				ffmpeg.Text = Radio.dep.path + "/ffmpeg.exe";
				Radio.config.Set("ffmpegPath", removeQuotesSingle(ffmpeg.Text));
				Radio.config.Save();
			});
		});
		
		void save(){	
			Radio.config.Set("ffmpegPath", removeQuotesSingle(ffmpeg.Text));
			Radio.config.Set("ytdlpPath", removeQuotesSingle(ytdlp.Text));
			
			Radio.config.Save();
		}
		
		TuiButton done = new TuiButton("Done", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			save();
		});
		
		TuiSelectable[,] t = OperatingSystem.IsWindows() ? new TuiSelectable[,]{{
			ffmpeg, ffmpeg
		},{
			ytdlp, ytdlp
		},{
			auto, auto
		},{
			open1, open2
		},{
			done, done
		}} : new TuiSelectable[,]{{
			ffmpeg, ffmpeg
		},{
			ytdlp, ytdlp
		},{
			open1, open2
		},{
			done, done
		}};
		
		TuiScreenInteractive l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Paths config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("FFMPEG path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("YT-DLP path:", Placement.TopLeft, 2, 9));
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			save();
			closeMiddleScreen();
		});
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setMiscConfig(){
		TuiFramedCheckBox usercp = new TuiFramedCheckBox(' ', 'X', !Radio.config.TryGetValue("dcrcp", out bool b) || b, Placement.TopCenter, 6, 4, null, null, null, Palette.user, Palette.user);
		
		TuiButton openAppdata = new TuiButton("Open appdata folder", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
			openFolder(Radio.dep.path);
		});
		
		TuiButton reset = new TuiButton("Reset", Placement.BottomCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
			Radio.config.Set("dcrcp", true);
			Radio.config.Save();
			
			usercp.Checked = true;
		});
		
		bool save(){
			Radio.config.Set("dcrcp", usercp.Checked);
			Radio.config.Save();
			
			if(usercp.Checked){
				if(Radio.dcrcp == null){
					Radio.dcrcp = new DiscordPresence();
				}
			}else{
				Radio.dcrcp?.Dispose();
				Radio.dcrcp = null;
			}
			
			return true;
		}
		
		TuiButton done = new TuiButton("Done", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			save();
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			usercp
		},{
			openAppdata
		},{
			reset
		},{
			done
		}};
		
		TuiScreenInteractive l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Miscellaneous config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Use Discord RCP:", Placement.TopCenter, -4, 5));
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	//Prepare textbox
	static TuiFramedTextBox setUFloat(TuiFramedTextBox b){
		b.CanWriteChar = c => {
			if(b.Text.Length + 1 > b.Length){
				return null;
			}
			if(char.IsDigit(c) || c == '.'){
				return c.ToString();
			}
			return null;
		};
		
		return b;
	}
	
	//Prepare textbox
	static TuiFramedTextBox setColor3(TuiFramedTextBox b){
		b.CanWriteChar = c => {
			if(b.Text.Length + 1 > b.Length){
				return null;
			}
			if(Uri.IsHexDigit(c) || c == '#'){
				return c.ToString();
			}
			return null;
		};
		
		return b;
	}
}