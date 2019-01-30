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
            RichPresence ret = new RichPresence() {
                Assets = new Assets() {
                    LargeImageKey = "menu"
                }
            };

            int? menuId = GameHelper.GetMenu();

            if (menuId.HasValue) {
                ret.Details = GameHelper.MenuToStringTop(menuId.Value);
                ret.State = GameHelper.MenuToStringBottom(menuId.Value);

                if (menuId == 27 /* Puzzle League Lobby */ || menuId == 28 /* Free Play Lobby */) {
                    ret.Details += $" ({GameHelper.LobbySize()} / {GameHelper.LobbyMax()})";
                }

                return ret;
            }

            if (GameHelper.IsAdventure()) {
                ret.Details = "Adventure";
                ret.Assets.LargeImageKey = "adventure";
                return ret;
            }

            if (GameHelper.IsInitial()) {
                ret.Details = "Splash Screen";
                return ret;
            }

            int majorId = GameHelper.GetMajorFromFlag();
            int modeId = GameHelper.GetMode(majorId);
            ret.Details = GameHelper.MajorToString(majorId);
            ret.Assets.LargeImageText = GameHelper.ModeToString(modeId);
            ret.Assets.LargeImageKey = GameHelper.ModeToImage(modeId);

            if (GameHelper.GetOnlineType() == 1 /* Free Play */) {
                ret.Details += $" ({GameHelper.LobbySize()} / {GameHelper.LobbyMax()})";
            }

            if (GameHelper.IsCharacterSelect()) {
                ret.State = "Character Select";
                return ret;
            }

            if (GameHelper.IsLoading()) {
                ret.State = "Loading";
                return ret;
            }

            int playerId = GameHelper.FindPlayer();

            string type = (modeId == 0 || modeId == 5 || modeId == 3 || modeId == 8 || modeId == 4 || modeId == 9)
                ? $" - {GameHelper.TypeToString(playerId)}"
                : "";

            int characterId = GameHelper.GetCharacter(playerId);

            ret.Assets.SmallImageText = GameHelper.CharacterToString(characterId);
            ret.Assets.SmallImageKey = GameHelper.CharacterToImage(characterId);

            if (GameHelper.IsPregame()) {
                ret.State = "Pregame";
                ret.Assets.LargeImageText += type;
                return ret;
            }

            if (GameHelper.IsMatch()) {
                ret.State = (GameHelper.LobbySize() == 2)
                    ? $"vs. {GameHelper.MatchPlayerName(1 - playerId)}"
                    : "Match";

                if (majorId == 4) ret.State += $" ({GameHelper.GetScore()})";

                ret.Assets.LargeImageText += type;

                return ret;
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

            } else {
                Presence.ClearPresence();
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

            Presence.ClearPresence();
            Presence.Dispose();
        }
    }
}
