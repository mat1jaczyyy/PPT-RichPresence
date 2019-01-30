﻿using System;
using System.Threading;

using DiscordRPC;

namespace PPT_RichPresence {
    static class Program {
        public static ProcessMemory PPT = new ProcessMemory("puyopuyotetris");
        static DiscordRpcClient Presence;

        static System.Windows.Forms.NotifyIcon tray = new System.Windows.Forms.NotifyIcon {
            ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] {
                new System.Windows.Forms.MenuItem("Copy Invite Link", new EventHandler(CopyInviteLink)),
                new System.Windows.Forms.MenuItem("-"),
                new System.Windows.Forms.MenuItem("Exit", new EventHandler(Close))
            }),
            Icon = Properties.Resources.TrayIcon,
            Text = "Puyo Puyo Tetris Rich Presence",
            Visible = true
        };

        static void CheckFreePlayLobby(object sender, EventArgs e) {
            ((System.Windows.Forms.ContextMenu)sender).MenuItems[0].Enabled = GameHelper.GetMenu() == 28;
        }

        static void CopyInviteLink(object sender, EventArgs e) {
            int? menuId = GameHelper.GetMenu();

            if (menuId.HasValue) {
                if (menuId == 28 /* Free Play Lobby */) {
                    System.Windows.Forms.Clipboard.SetText(GameHelper.LobbyInvite());
                }
            }
        }

        static void Close(object sender, EventArgs e) {
            System.Windows.Forms.Application.Exit();
        }

        static Timer ScanTimer = new Timer(new TimerCallback(Loop), null, Timeout.Infinite, 1000);

        static RichPresence GetState() {
            int? menuId = GameHelper.GetMenu();

            if (menuId.HasValue) {
                RichPresence ret = new RichPresence() {
                    Details = GameHelper.MenuToStringTop(menuId.Value),
                    State = GameHelper.MenuToStringBottom(menuId.Value),
                    Assets = new Assets() {
                        LargeImageKey = "menu"
                    },
                };

                if (menuId == 27 /* Puzzle League Lobby */ || menuId == 28 /* Free Play Lobby */) ret.Party = new Party() {
                    ID = Guid.Empty.ToString(),
                    Size = GameHelper.LobbySize(),
                    Max = GameHelper.LobbyMax()
                };

                return ret;
            }

            if (GameHelper.IsAdventure()) {
                return new RichPresence() {
                    Details = "Adventure",
                    Assets = new Assets() {
                        LargeImageKey = "adventure"
                    }
                };
            }

            if (GameHelper.IsInitial()) {
                return new RichPresence() {
                    Details = "Splash Screen",
                    Assets = new Assets() {
                        LargeImageKey = "menu"
                    }
                };
            }

            int majorId = GameHelper.GetMajorFromFlag();
            int modeId = GameHelper.GetMode(majorId);
            string details = GameHelper.MajorToString(majorId);
            string largetext = GameHelper.ModeToString(modeId);
            string largekey = GameHelper.ModeToImage(modeId);

            if (GameHelper.IsCharacterSelect()) {
                return new RichPresence() {
                    Details = details,
                    State = "Character Select",
                    Assets = new Assets() {
                        LargeImageKey = largekey,
                        LargeImageText = largetext
                    }
                };
            }

            if (GameHelper.IsLoading()) {
                return new RichPresence() {
                    Details = details,
                    State = "Loading",
                    Assets = new Assets() {
                        LargeImageKey = largekey,
                        LargeImageText = largetext
                    }
                };
            }

            if (GameHelper.InMatch()) {
                string type = (modeId == 0 || modeId == 5 || modeId == 3 || modeId == 8 || modeId == 4 || modeId == 9)
                    ? $" - {GameHelper.TypeToString(GameHelper.GetType(GameHelper.FindPlayer()))}"
                    : "";

                return new RichPresence() {
                    Details = details,
                    State = "Match",
                    Assets = new Assets() {
                        LargeImageKey = largekey,
                        LargeImageText = largetext + type
                    }
                };
            }

            return new RichPresence() {
                Assets = new Assets() {
                    LargeImageKey = "menu"
                }
            };
        }

        static void Loop(object e) {
            Presence.Invoke();
            
            if (PPT.CheckProcess()) {
                PPT.TrustProcess = true;
                Presence.SetPresence(GetState());
                PPT.TrustProcess = false;
            }
        }

        [STAThread]
        static void Main() {
            Presence = new DiscordRpcClient("539426896841277440");
            //Presence.OnReady += (sender, e) => {};
            Presence.Initialize();

            tray.ContextMenu.Popup += CheckFreePlayLobby;

            ScanTimer.Change(0, 1000);

            System.Windows.Forms.Application.Run();

            ScanTimer.Dispose();

            Presence.Dispose();
        }
    }
}
