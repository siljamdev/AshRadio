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
		if(currentMiddleScreen.identifier == "config"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			new TuiButton("Palette", Placement.TopCenter, 0, 4, null, Palette.user).SetAction((s, ck) => {
				setPaletteConfig();
			})
		},{
			new TuiButton("UI", Placement.TopCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
				setUiConfig();
			})
		},{
			new TuiButton("Player", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				setPlayerConfig();
			})
		},{
			new TuiButton("Paths", Placement.TopCenter, 0, 10, null, Palette.user).SetAction((s, ck) => {
				setPathConfig();
			})
		},{
			new TuiButton("Miscellaneous", Placement.TopCenter, 0, 12, null, Palette.user).SetAction((s, ck) => {
				setMiscConfig();
			})
		},{
			new TuiButton("Reset config", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
				setConfirmScreen("Do you want to reset the whole config?", () => {
					Radio.resetConfig();
					
					init();
					
					if(Radio.config.GetValue<bool>("dcrp")){
						if(Radio.dcrpc == null){
							Radio.dcrpc = new DiscordPresence();
						}
					}else{
						Radio.dcrpc?.Dispose();
						Radio.dcrpc = null;
					}
				});
			})
		}};
		
		MiddleScreen l = generateMiddle(t);
		l.identifier = "config";
		
		l.interactive.Elements.Add(new TuiLabel("Config", Placement.TopCenter, 0, 1, Palette.main));
		l.interactive.Elements.Add(new TuiTwoLabels("AshRadio v" + Radio.version, " made by siljam", Placement.BottomRight, 0, 0, Palette.hint, null));
		
		setMiddleScreen(l);
	}
	
	void setPaletteConfig(){
		setMiddleScreen(generateConfigScreen("Palette", 8, new (string, ConfigType, string, int)[]{
				("ui.palette.user.fg", ConfigType.Color, "User color foreground", 0),
				("ui.palette.user.bg", ConfigType.Color, "User color background", 0),
				
				("ui.palette.writing.fg", ConfigType.Color, "Writing color foreground", 0),
				("ui.palette.writing.bg", ConfigType.Color, "Writing color background", 0),
				
				("ui.palette.main.fg", ConfigType.Color, "Main color foreground", 0),
				("ui.palette.main.bg", ConfigType.Color, "Main color background", 0),
				
				("ui.palette.song.fg", ConfigType.Color, "Song color foreground", 0),
				("ui.palette.song.bg", ConfigType.Color, "Song color background", 0),
				
				("ui.palette.author.fg", ConfigType.Color, "Author color foreground", 0),
				("ui.palette.author.bg", ConfigType.Color, "Author color background", 0),
				
				("ui.palette.playlist.fg", ConfigType.Color, "Playlist color foreground", 0),
				("ui.palette.playlist.bg", ConfigType.Color, "Playlist color background", 0),
				
				("ui.palette.info.fg", ConfigType.Color, "Info color foreground", 0),
				("ui.palette.info.bg", ConfigType.Color, "Info color background", 0),
				
				("ui.palette.hint.fg", ConfigType.Color, "Hint color foreground", 0),
				("ui.palette.hint.bg", ConfigType.Color, "Hint color background", 0),
				
				("ui.palette.delimiter.fg", ConfigType.Color, "Delimiter color foreground", 0),
				("ui.palette.delimiter.bg", ConfigType.Color, "Delimiter color background", 0),
				
				("ui.palette.error.fg", ConfigType.Color, "Error color foreground", 0),
				("ui.palette.error.bg", ConfigType.Color, "Error color background", 0),
				
				("ui.palette.selectedDefault.fg", ConfigType.Color, "Selected panel color foreground", 0),
				("ui.palette.selectedDefault.bg", ConfigType.Color, "Selected panel color background", 0),
				
				("ui.palette.default.fg", ConfigType.Color, "Default color foreground", 0),
				("ui.palette.default.bg", ConfigType.Color, "Default color background", 0)
			},
			new (string, Action<TuiSelectable, ConsoleKeyInfo>)[]{
				("Ash Palette", (s, ck) => {
					Palette.setAsh();
					closeMiddleScreen(); //update
					setPaletteConfig();
				}),
				("Subtle Palette", (s, ck) => {
					Palette.setSubtle();
					closeMiddleScreen(); //update
					setPaletteConfig();
				}),
				("Neon Palette", (s, ck) => {
					Palette.setNeon();
					closeMiddleScreen(); //update
					setPaletteConfig();
				}),
				("Light Palette", (s, ck) => {
					Palette.setLight();
					closeMiddleScreen(); //update
					setPaletteConfig();
				}),
			},
			() => {
				Palette.init();
			}
		));
	}
	
	void setUiConfig(){
		setMiddleScreen(generateConfigScreen("UI", 8, new (string, ConfigType, string, int)[]{
				("ui.useColors", ConfigType.Bool, "Use colors", 0),
				("ui.cursorBlinks", ConfigType.Bool, "Cursor blinks", 1),
				("ui.cursor", ConfigType.String, "Cursor char", 1),
				("ui.selectors", ConfigType.String, "Selector chars", 2),
			},
			new (string, Action<TuiSelectable, ConsoleKeyInfo>)[0],
			() => {
				init();
			}
		));
	}
	
	void setPlayerConfig(){
		setMiddleScreen(generateConfigScreen("Player", 8, new (string, ConfigType, string, int)[]{
				("player.volumeExponent", ConfigType.Ufloat, "Volume correction exponent", 8),
				("player.advanceTime", ConfigType.Ufloat, "Advance time", 8),
			},
			new (string, Action<TuiSelectable, ConsoleKeyInfo>)[0],
			() => {
				Radio.py.volumeExponent = Radio.config.GetValue<float>("player.advanceTime");
			}
		));
	}
	
	void setPathConfig(){
		setMiddleScreen(generateConfigScreen("Paths", 16, new (string, ConfigType, string, int)[]{
				("ffmpegPath", ConfigType.Path, "FFMPEG path", 256),
				("ffprobePath", ConfigType.Path, "FFPROBE path", 256),
				("ytdlpPath", ConfigType.Path, "YT-DLP path", 256)
			},
			new (string, Action<TuiSelectable, ConsoleKeyInfo>)[]{
				("Open ffmpeg.org", (s, ck) => {
					openUrl("https://ffmpeg.org/");
				}),
				("Open yt-dlp downloads", (s, ck) => {
					openUrl("https://github.com/yt-dlp/yt-dlp/releases");
				}),
				("Auto dl ffmpeg & ffprobe", (s, ck) => {
					TuiButton b = ((TuiButton) s);
					MiddleScreen midsc = currentMiddleScreen;
					if(Radio.downloadFfmpeg(() => {
						if(currentMiddleScreen == midsc){
							closeMiddleScreen();
							setPathConfig();
						}
					})){
						b.Text = "Downloading…";
					}
				}),
				("Auto dl yt-dlp", (s, ck) => {
					TuiButton b = ((TuiButton) s);
					MiddleScreen midsc = currentMiddleScreen;
					if(Radio.downloadYtdlp(() => {
						if(currentMiddleScreen == midsc){
							closeMiddleScreen();
							setPathConfig();
						}
					})){
						b.Text = "Downloading…";
					}
				}),
				("Auto download all", (s, ck) => {
					TuiButton b = ((TuiButton) s);
					MiddleScreen midsc = currentMiddleScreen;
					bool b1 = Radio.downloadFfmpeg(() => {
						if(currentMiddleScreen == midsc){
							closeMiddleScreen();
							setPathConfig();
						}
					});
					
					bool b2 = Radio.downloadYtdlp(() => {
						if(currentMiddleScreen == midsc){
							closeMiddleScreen();
							setPathConfig();
						}
					});
					
					if(b1 || b2){
						b.Text = "Downloading…";
					}
				}),
				("Auto update yt-dlp", (s, ck) => {
					Radio.updateYtdlp();
				})
			},
			null
		));
	}
	
	void setMiscConfig(){
		setMiddleScreen(generateConfigScreen("Miscellaneous", 8, new (string, ConfigType, string, int)[]{
				("dcrp", ConfigType.Bool, "Use Discord RPC", 0)
			},
			new (string, Action<TuiSelectable, ConsoleKeyInfo>)[]{
				("Attempt id repair", (s, ck) => {
					Song.repairLatestId();
					Author.repairLatestId();
					Playlist.repairLatestId();
				}),
				("Open data directory", (s, ck) => {
					openFolder(Radio.dep.path);
				}),
				("Open config directory", (s, ck) => {
					openFolder(Radio.appDataPath);
				}),
			},
			() => {
				if(Radio.config.GetValue<bool>("dcrp")){
					if(Radio.dcrpc == null){
						Radio.dcrpc = new DiscordPresence();
					}
				}else{
					Radio.dcrpc?.Dispose();
					Radio.dcrpc = null;
				}
			}
		));
	}
											//config .ash name, type, description, length		//Right actions
	MiddleScreen generateConfigScreen(string title, int fieldsize, (string, ConfigType, string, int)[] configs, (string, Action<TuiSelectable, ConsoleKeyInfo>)[] actions, Action onSave){
		//Helper method
		string getColorString(string key){
			if(Radio.config.TryGetValue(key, out Color3 cf)){
				return cf.ToString();
			}else{
				return "";
			}
		}
		
		MiddleScreen midsc = null!;
		
		//Config fields
		TuiSelectable[] configFields = new TuiSelectable[configs.Length];
		TuiLabel[] configDescriptions = new TuiLabel[configs.Length];
		
		int y = 1;
		
		for(int i = 0; i < configs.Length; i++){
			(string key, ConfigType type, string desc, int len) = configs[i];
			
			configDescriptions[i] = new TuiLabel(desc + ":", Placement.TopLeft, 1, y - 1);
			
			switch(type){
				case ConfigType.Bool:
					configFields[i] = new TuiFramedCheckBox(' ', 'X', Radio.config.GetValue<bool>(key), Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user);
					y += 5;
					break;
				
				case ConfigType.Int:
					configFields[i] = setInt(new TuiFramedScrollingTextBox(Radio.config.GetValue<int>(key).ToString(), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user));
					y += 5;
					break;
				
				case ConfigType.Uint:
					configFields[i] = setUint(new TuiFramedScrollingTextBox(Radio.config.GetValue<int>(key).ToString(), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user));
					y += 5;
					break;
				
				case ConfigType.Float:
					configFields[i] = setFloat(new TuiFramedScrollingTextBox(Radio.config.GetValue<float>(key).ToString(), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user));
					y += 5;
					break;
				
				case ConfigType.Ufloat:
					configFields[i] = setUfloat(new TuiFramedScrollingTextBox(Radio.config.GetValue<float>(key).ToString(), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user));
					y += 5;
					break;
				
				case ConfigType.Color:
					configFields[i] = new TuiColor3TextBox(getColorString(key), fieldsize, Placement.TopLeft, 1, y, key.EndsWith(".bg"), null, Palette.user, Palette.user);
					y += 5;
					break;
				
				case ConfigType.String:
				case ConfigType.Path:
					configFields[i] = new TuiFramedScrollingTextBox(Radio.config.GetValue<string>(key), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user);
					y += 5;
					break;
			}
		}
		
		//Actions
		TuiButton[] actionButtons = new TuiButton[actions.Length + 1];
		
		for(int i = 0; i < actions.Length; i++){
			(string name, Action<TuiSelectable, ConsoleKeyInfo> act) = actions[i];
			actionButtons[i] = new TuiButton(name, Placement.TopCenter, 7, 2 + 2 * i, null, Palette.user).SetAction(act);
		}
		
		//Reset button
		actionButtons[actions.Length] = new TuiButton("Reset", Placement.BottomCenter, 5, 2, null, Palette.user).SetAction((s, ck) => {
			for(int i = 0; i < configs.Length; i++){
				(string key, ConfigType type, _, _) = configs[i];
				ModelInstance ins = Radio.configModel.instances.FirstOrDefault(h => h.name == key);
				if(ins != null){
					Radio.config.Set(key, ins.value);
				}
			}
			
			Radio.config.Save();
			
			updateMiddleScreen(midsc, () => {
				return generateConfigScreen(title, fieldsize, configs, actions, onSave);
			});
		});
		
		TuiLabel errorLabel = new TuiLabel("", Placement.BottomCenter, 0, 2, Palette.error);
		
		bool save(){
			object[] results = new object[configs.Length];
			
			for(int i = 0; i < configs.Length; i++){
				(_, ConfigType type, string desc, _) = configs[i];
				switch(type){
					case ConfigType.Bool:
						results[i] = ((TuiCheckBox) configFields[i]).Checked;
						break;
					
					case ConfigType.Int:
						if(int.TryParse(((TuiWritable) configFields[i]).Text, out int in2)){
							results[i] = in2;
						}else{
							errorLabel.Text = "Invalid " + desc + " number";
							return false;
						}
						break;
					
					case ConfigType.Uint:
						if(uint.TryParse(((TuiWritable) configFields[i]).Text, out uint ui)){
							results[i] = (int) ui;
						}else{
							errorLabel.Text = "Invalid " + desc + " number";
							return false;
						}
						break;
					
					case ConfigType.Float:
					case ConfigType.Ufloat:
						if(float.TryParse(((TuiWritable) configFields[i]).Text, out float f)){
							results[i] = f;
						}else{
							errorLabel.Text = "Invalid " + desc + " number";
							return false;
						}
						break;
					
					case ConfigType.Color:
						if(Color3.TryParse(((TuiWritable) configFields[i]).Text, out Color3 c3)){
							results[i] = c3;
						}else if(((TuiWritable) configFields[i]).Text.Length == 0){
							results[i] = false;
						}else{
							errorLabel.Text = "Invalid " + desc + " color";
							return false;
						}
						break;
					
					case ConfigType.String:
						results[i] = ((TuiWritable) configFields[i]).Text;
						break;
					
					case ConfigType.Path:
						results[i] = removeQuotesSingle(((TuiWritable) configFields[i]).Text);
						break;
				}
			}
			
			for(int i = 0; i < configs.Length; i++){
				(string key, _, _, _) = configs[i];
				Radio.config.Set(key, results[i]);
			}
			
			Radio.config.Save();
			
			errorLabel.Text = "";
			onSave?.Invoke();
			return true;
		}
		
		TuiButton done = new TuiButton("Done", Placement.CenterRight, 2, 0, Palette.info, Palette.user).SetAction((s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		//TuiSelectable matrix population
		int lenw = Math.Max(configFields.Length, actionButtons.Length);
		TuiSelectable[,] t = new TuiSelectable[lenw, 3];
		
		for(int i = 0; i < lenw; i++){
			t[i, 0] = configFields[i % configFields.Length];
			t[i, 1] = actionButtons[i % actionButtons.Length];
			t[i, 2] = done;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Config - " + title, Placement.TopCenter, 0, 1, Palette.main));
		
		backg.Elements.Add(errorLabel);
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 6, 0),
			t, 0, 0,
			Placement.TopLeft, 3, 4,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 6, 0);
		};
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		l.Elements.AddRange(configDescriptions);
		
		l.FixedElements.AddRange(actionButtons);
		l.FixedElements.Add(done);
		
		midsc = new MiddleScreen(backg, l);
		
		return midsc;
	}
	
	//Prepare textbox
	static TuiWritable setInt(TuiWritable b){
		b.CanWriteChar = c => {
			if(b.Text.Length + 1 > b.Length){
				return null;
			}
			if(char.IsDigit(c) || c == '-'){
				return c.ToString();
			}
			return null;
		};
		
		return b;
	}
	
	//Prepare textbox
	static TuiWritable setUint(TuiWritable b){
		b.CanWriteChar = c => {
			if(b.Text.Length + 1 > b.Length){
				return null;
			}
			if(char.IsDigit(c)){
				return c.ToString();
			}
			return null;
		};
		
		return b;
	}
	
	//Prepare textbox
	static TuiWritable setFloat(TuiWritable b){
		b.CanWriteChar = c => {
			if(b.Text.Length + 1 > b.Length){
				return null;
			}
			if(char.IsDigit(c) || c == '.' || c == '-'){
				return c.ToString();
			}
			return null;
		};
		
		return b;
	}
	
	//Prepare textbox
	static TuiWritable setUfloat(TuiWritable b){
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
}

enum ConfigType{
	Bool, Int, Uint, Float, Ufloat, Color, String, Path
}