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
	TuiMultipleLabels creditsLabel = new TuiMultipleLabels(new string[]{"AshRadio", " made by ", "siljam"}, Placement.BottomRight, 0, 0, new CharFormat?[]{Palette.hint, null, Palette.hint});
	
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
			new TuiButton("UI", Placement.TopCenter, 0, 5, null, Palette.user).SetAction((s, ck) => {
				setUiConfig();
			})
		},{
			new TuiButton("Keybinds", Placement.TopCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
				setKeybindsConfig();
			})
		},{
			new TuiButton("Player", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				setPlayerConfig();
			})
		},{
			new TuiButton("Paths", Placement.TopCenter, 0, 9, null, Palette.user).SetAction((s, ck) => {
				setPathConfig();
			})
		},{
			new TuiButton("Miscellaneous", Placement.TopCenter, 0, 10, null, Palette.user).SetAction((s, ck) => {
				setMiscConfig();
			})
		},{
			new TuiButton("About", Placement.TopCenter, 0, 12, null, Palette.user).SetAction((s, ck) => {
				setAbout();
			})
		},{
			new TuiButton("Reset config", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
				setConfirmScreen("Do you want to reset the whole config?", () => {
					Radio.resetConfig();
					
					Radio.py.init();
					Radio.py.setVolume(Radio.py.volume);
					
					if(Radio.config.GetValue<bool>("dcrp")){
						if(Radio.dcrpc == null){
							Radio.dcrpc = new DiscordPresence();
						}
					}else{
						Radio.dcrpc?.Dispose();
						Radio.dcrpc = null;
					}
					
					reinitScreens();
				});
			})
		}};
		
		MiddleScreen l = generateMiddle(t);
		l.identifier = "config";
		
		l.interactive.Elements.Add(new TuiLabel("Config", Placement.TopCenter, 0, 1, Palette.main));
		l.interactive.Elements.Add(creditsLabel);
		
		setMiddleScreen(l);
	}
	
	void setPaletteConfig(){
		setMiddleScreen(generateConfigScreen("Palette", 8, new (string, ConfigType, string, int)[]{
				("ui.palette.user", ConfigType.Color, "User color", 0),
				("ui.palette.writing", ConfigType.Color, "Writing color", 0),
				("ui.palette.main", ConfigType.Color, "Main color", 0),
				("ui.palette.song", ConfigType.Color, "Song color", 0),
				("ui.palette.author", ConfigType.Color, "Author color", 0),
				("ui.palette.playlist", ConfigType.Color, "Playlist color", 0),
				("ui.palette.info", ConfigType.Color, "Info color", 0),
				("ui.palette.hint", ConfigType.Color, "Hint color", 0),
				("ui.palette.delimiter", ConfigType.Color, "Delimiter color", 0),
				("ui.palette.error", ConfigType.Color, "Error color", 0),
				
				("ui.palette.selectedDefault", ConfigType.Color, "Selected panel color", 0),
				("ui.palette.default", ConfigType.Color, "Default color", 0)
			},
			new (string, Action<TuiSelectable, ConsoleKeyInfo>)[]{
				("Ash Palette", (s, ck) => {
					Palette.reset();
					reinitScreens();
				}),
				("Subtle Palette", (s, ck) => {
					Palette.setSubtle();
					reinitScreens();
				}),
				("Neon Palette", (s, ck) => {
					Palette.setNeon();
					reinitScreens();
				}),
				("Light Palette", (s, ck) => {
					Palette.setLight();
					reinitScreens();
				}),
			},
			() => {
				reinitScreens();
			}
		));
	}
	
	void setUiConfig(){
		setMiddleScreen(generateConfigScreen("UI", 8, new (string, ConfigType, string, int)[]{
				("ui.useColors", ConfigType.Bool, "Use colors", 0),
				("ui.cursor", ConfigType.String, "Cursor char", 1),
				("ui.cursorBlinks", ConfigType.Bool, "Cursor blinks", 1),
				("ui.cursorBlinkPeriod", ConfigType.Ufloat, "Cursor blik period", 6),
				("ui.playingChars", ConfigType.String, "Playing/Paused chars", 2),
				("ui.selectors", ConfigType.String, "Selector chars", 2),
				("ui.updateFrequency", ConfigType.Ufloat, "Update frequency", 6),
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
				("player.advanceTime", ConfigType.Ufloat, "Advance time (seconds)", 8),
			},
			new (string, Action<TuiSelectable, ConsoleKeyInfo>)[0],
			() => {
				Radio.py.init();
				Radio.py.setVolume(Radio.py.volume);
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
							setPathConfig();
							updateMiddleScreen(midsc, () => null);
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
							setPathConfig();
							updateMiddleScreen(midsc, () => null);
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
							setPathConfig();
							updateMiddleScreen(midsc, () => null);
						}
					});
					
					bool b2 = Radio.downloadYtdlp(() => {
						if(currentMiddleScreen == midsc){
							setPathConfig();
							updateMiddleScreen(midsc, () => null);
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
		setMiddleScreen(generateConfigScreen("Miscellaneous", 16, new (string, ConfigType, string, int)[]{
				("dcrp", ConfigType.Bool, "Use Discord RPC", 0),
				("osmediaintegration", ConfigType.Bool, "Use OS Media integration", 0),
				#if LINUX
					("osmediaintegration.linuxdesktop", ConfigType.String, "AshRadio .desktop file name", 256),
				#endif
				("capErrorLogs", ConfigType.Bool, "Cap error logs at 5", 0)
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
				("See error log", (s, ck) => {
					setErrorLog();
				})
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
	
	void setKeybindsConfig(){
		setMiddleScreen(generateControlsScreen(Keybinds.configurables,
			() => {
				reinitScreens();
			}
		));
	}
	
											//config .ash name, type, description, length		//Right actions
	MiddleScreen generateConfigScreen(string title, int fieldsize, (string, ConfigType, string, int)[] configs, (string, Action<TuiSelectable, ConsoleKeyInfo>)[] actions, Action onSave, bool renderFocused = false){
		//Helper method
		string getColorFgString(string key){
			if(Radio.config.TryGetValue(key, out Color3 cf)){
				return cf.ToString();
			}else if(Radio.config.TryGetValue(key, out Color3[] ca)){
				if(ca.Length == 2){
					return ca[0].ToString();
				}
			}
			return "";
		}
		
		//Helper method
		string getColorBgString(string key){
			if(Radio.config.TryGetValue(key, out Color3[] ca)){
				if(ca.Length == 2){
					return ca[1].ToString();
				}else if(ca.Length == 1){
					return ca[0].ToString();
				}
			}
			return "";
		}
		
		MiddleScreen midsc = null!;
		
		//Config fields
		List<TuiSelectable> configFields = new(configs.Length);
		List<TuiLabel> configDescriptions = new(configs.Length);
		
		int y = 1;
		
		for(int i = 0; i < configs.Length; i++){
			(string key, ConfigType type, string desc, int len) = configs[i];
			
			configDescriptions.Add(new TuiLabel(desc + ":", Placement.TopLeft, 1, y - 1));
			
			switch(type){
				case ConfigType.Bool:
					configFields.Add(new TuiFramedCheckBox(' ', 'X', Radio.config.GetValue<bool>(key), Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user));
					y += 5;
					break;
				
				case ConfigType.Int:
					configFields.Add(setInt(new TuiFramedScrollingTextBox(Radio.config.GetValue<int>(key).ToString(), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user)));
					y += 5;
					break;
				
				case ConfigType.Uint:
					configFields.Add(setUint(new TuiFramedScrollingTextBox(Radio.config.GetValue<int>(key).ToString(), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user)));
					y += 5;
					break;
				
				case ConfigType.Float:
					configFields.Add(setFloat(new TuiFramedScrollingTextBox(Radio.config.GetValue<float>(key).ToString(), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user)));
					y += 5;
					break;
				
				case ConfigType.Ufloat:
					configFields.Add(setUfloat(new TuiFramedScrollingTextBox(Radio.config.GetValue<float>(key).ToString(), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user)));
					y += 5;
					break;
				
				case ConfigType.Color:
					configDescriptions.Add(new TuiLabel("Foreground:", Placement.TopLeft, 2, y));
					y++;
					configFields.Add(new TuiColor3TextBox(getColorFgString(key), fieldsize, Placement.TopLeft, 1, y, false, null, Palette.user, Palette.user));
					y += 3;
					configDescriptions.Add(new TuiLabel("Background:", Placement.TopLeft, 2, y));
					y++;
					configFields.Add(new TuiColor3TextBox(getColorBgString(key), fieldsize, Placement.TopLeft, 1, y, true, null, Palette.user, Palette.user));
					y+= 5;
					break;
				
				case ConfigType.String:
				case ConfigType.Path:
					configFields.Add(new TuiFramedScrollingTextBox(Radio.config.GetValue<string>(key), len, fieldsize, Placement.TopLeft, 1, y, null, null, null, Palette.writing, Palette.user, Palette.user));
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
		actionButtons[actions.Length] = new TuiButton("Reset", actions.Length == 0 ? Placement.Center : Placement.BottomCenter, 5, actions.Length == 0 ? 0 : 2, null, Palette.user).SetAction((s, ck) => {
			for(int i = 0; i < configs.Length; i++){
				(string key, ConfigType type, _, _) = configs[i];
				ModelInstance ins = Radio.configModel.instances.FirstOrDefault(h => h.name == key);
				if(ins != null){
					Radio.config.Set(key, ins.value);
				}
			}
			
			Radio.config.Save();
			
			updateMiddleScreen(midsc, () => {
				return generateConfigScreen(title, fieldsize, configs, actions, onSave, true);
			});
		});
		
		TuiLabel errorLabel = new TuiLabel("", Placement.BottomCenter, 0, 2, Palette.error);
		
		bool save(){
			object[] results = new object[configs.Length];
			
			int j = 0;
			
			for(int i = 0; i < configs.Length; i++, j++){
				(_, ConfigType type, string desc, _) = configs[i];
				switch(type){
					case ConfigType.Bool:
						results[i] = ((TuiCheckBox) configFields[j]).Checked;
						break;
					
					case ConfigType.Int:
						if(int.TryParse(((TuiWritable) configFields[j]).Text, out int in2)){
							results[i] = in2;
						}else{
							errorLabel.Text = "Invalid " + desc + " number";
							return false;
						}
						break;
					
					case ConfigType.Uint:
						if(uint.TryParse(((TuiWritable) configFields[j]).Text, out uint ui)){
							results[i] = (int) ui;
						}else{
							errorLabel.Text = "Invalid " + desc + " number";
							return false;
						}
						break;
					
					case ConfigType.Float:
					case ConfigType.Ufloat:
						if(float.TryParse(((TuiWritable) configFields[j]).Text, out float f)){
							results[i] = f;
						}else{
							errorLabel.Text = "Invalid " + desc + " number";
							return false;
						}
						break;
					
					case ConfigType.Color:
						Color3? fg = null;
						if(Color3.TryParse(((TuiWritable) configFields[j]).Text, out Color3 c1)){
							fg = c1;
						}else if(((TuiWritable) configFields[j]).Text.Length != 0){
							errorLabel.Text = "Invalid " + desc + " foreground color";
							return false;
						}
						
						j++;
						
						Color3? bg = null;
						if(Color3.TryParse(((TuiWritable) configFields[j]).Text, out Color3 c2)){
							bg = c2;
						}else if(((TuiWritable) configFields[j]).Text.Length != 0){
							errorLabel.Text = "Invalid " + desc + " background color";
							return false;
						}
						
						if(fg == null){
							if(bg == null){
								results[i] = new Color3[0];
							}else{
								results[i] = new Color3[]{(Color3) bg};
							}
						}else{
							if(bg == null){
								results[i] = (Color3) fg;
							}else{
								results[i] = new Color3[]{(Color3) fg, (Color3) bg};
							}
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
		int lenw = Math.Max(configFields.Count, actionButtons.Length);
		TuiSelectable[,] t = new TuiSelectable[lenw, 3];
		
		for(int i = 0; i < lenw; i++){
			t[i, 0] = configFields[i % configFields.Count];
			t[i, 1] = actionButtons[i % actionButtons.Length];
			t[i, 2] = done;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Config - " + title, Placement.TopCenter, 0, 1, Palette.main));
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 6, 0),
			t, (uint) (renderFocused ? 1 : 0), (uint) (renderFocused ? actionButtons.Length - 1 : 0),
			Placement.TopLeft, 3, 4,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 6, 0);
		};
		
		midsc = new MiddleScreen(backg, l);
		
		Keybinds.escape.subEvent(midsc, false, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		backg.Elements.Add(errorLabel);
		backg.Elements.Add(creditsLabel);
		
		l.Elements.AddRange(configDescriptions);
		
		l.FixedElements.AddRange(actionButtons);
		l.FixedElements.Add(done);
		
		return midsc;
	}
	
	MiddleScreen generateControlsScreen(Keybind[] configs, Action onSave){
		MiddleScreen midsc = null!;
		
		//TuiSelectable matrix
		TuiSelectable[,] t = new TuiSelectable[Math.Max(1, configs.Length * 2), 3];
		
		//Reset button
		TuiButton reset = new TuiButton("Reset", Placement.Center, 5, 0, null, Palette.user).SetAction((s, ck) => {
			Keybinds.reset();
			
			reinitScreens();
		});
		
		bool save(){
			for(int i = 0; i < configs.Length; i++){
				List<byte> b = new();
				if(((TuiKeySelector) t[i * 2, 0]).key is (ConsoleKey k, ConsoleModifiers m)){
					b.Add((byte) k);
					b.Add((byte) m);
				}
				
				if(((TuiKeySelector) t[i * 2 + 1, 0]).key is (ConsoleKey k2, ConsoleModifiers m2)){
					b.Add((byte) k2);
					b.Add((byte) m2);
				}
				
				Radio.config.Set(configs[i].key, b.ToArray());
			}
			
			Radio.config.Save();
			
			onSave?.Invoke();
			return true;
		}
		
		TuiButton done = new TuiButton("Done", Placement.CenterRight, 2, 0, Palette.info, Palette.user).SetAction((s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		//TuiSelectable matrix population
		for(int i = 0; i < configs.Length; i++){
			t[i * 2, 0] = new TuiKeySelector(configs[i].primary, Placement.TopLeft, 13, 1 + i * 4, Palette.hint, Palette.hint, Palette.writing, Palette.user);
			t[i * 2 + 1, 0] = new TuiKeySelector(configs[i].secondary, Placement.TopLeft, 13, 2 + i * 4, Palette.hint, Palette.hint, Palette.writing, Palette.user);
			t[i * 2, 1] = reset;
			t[i * 2 + 1, 1] = reset;
			t[i * 2, 2] = done;
			t[i * 2 + 1, 2] = done;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Config - Keybinds", Placement.TopCenter, 0, 1, Palette.main));
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 6, 0),
			t, 0, 0,
			Placement.TopLeft, 3, 4,
			null
		);
		
		backg.Elements.Add(l);
		backg.Elements.Add(creditsLabel);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 6, 0);
		};
		
		midsc = new MiddleScreen(backg, l);
		
		Keybinds.escape.subEvent(midsc, false, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		//Descriptions
		for(int i = 0; i < configs.Length; i++){
			l.Elements.Add(new TuiLabel(configs[i].description + ":", Placement.TopLeft, 1, i * 4));
			l.Elements.Add(new TuiLabel("Primary:", Placement.TopLeft, 2, i * 4 + 1));
			l.Elements.Add(new TuiLabel("Secondary:", Placement.TopLeft, 2, i * 4 + 2));
		}
		
		l.FixedElements.Add(reset);
		l.FixedElements.Add(done);
		
		return midsc;
	}
	
	void setAbout(){
		if(currentMiddleScreen.identifier == "about"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		bool b2 = false;
		
		MiddleScreen l3 = generateMiddle(new TuiSelectable[,]{{
			new TuiButton("Check for updates", Placement.BottomCenter, 0, 4, null, Palette.user).SetAction((s, ck) => {
				if(b2){
					return;
				}
				TuiButton b = ((TuiButton) s);
				b.Text = "Checking…";
				b2 = true;
				Radio.fetchUpdate(async newVersion => {
					if(newVersion == null){
						b.TextFormat = Palette.error;
						b.SelectedTextFormat = Palette.error;
						b.Text = "An error occured!";
					}else if(isVersionEqual(newVersion)){
						b.Text = "Up to date";
					}else if(isVersionOlder(newVersion)){
						b.TextFormat = Palette.main;
						b.SelectedTextFormat = Palette.main;
						b.Text = "ARE YOU A TIME TRAVELER?!?!?!?";
					}else{
						b.Text = "New version available: " + newVersion;
					}
				});
			})
		},{
			new TuiButton("Open GitHub repo", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
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
	
	void setErrorLog(){
		if(currentMiddleScreen.identifier == "errorlog"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		MiddleScreen l3 = generateMiddle(null);
		l3.identifier = "errorlog";
		TuiScreenInteractive l = l3.interactive;
		
		TuiLog content = new TuiLog(l.Xsize - 4, l.Ysize - 7, Placement.TopLeft, 2, 5, Palette.error);
		
		content.OnParentResize += (s, a) => {
			content.Xsize = l.Xsize - 4;
			content.Ysize = l.Ysize - 7;
			
			content.ScrollToTop();
		};
		
		Keybinds.scrollUp.subEvent(l3, true, (s, ck) => {
			content.Scroll++;
		});
		
		Keybinds.scrollDown.subEvent(l3, true, (s, ck) => {
			content.Scroll--;
		});
		
		try{
			if(File.Exists(Radio.errorFilePath)){
				content.Append(File.ReadAllText(Radio.errorFilePath));
			}
		}catch(Exception e){
			Radio.reportError(e.ToString());
		}
		
		content.ScrollToTop();
		
		l.Elements.Add(new TuiLabel("Error log", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel(Radio.errorFilePath, Placement.TopCenter, 0, 3));
		l.Elements.Add(content);
		
		setMiddleScreen(l3);
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