﻿using System;
using System.Collections.Generic;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AfiaBot
{
    internal static class Menu
    {
        private const string str_newroom = "Создать комнату";
        private const string str_joinroom = "Присоединиться";
        private const string str_cancel = "Отмена";

        private static readonly Dictionary<long, int> playerRoom; //chat-room relation
        private static readonly Dictionary<int, GameRoom> rooms;

        private static readonly ReplyKeyboardMarkup markupMenu;
        private static readonly ReplyKeyboardMarkup markupCancel;
        private static ReplyKeyboardHide markupHide;

        private static bool waitingForRoomId; //flag to recieve answer

        static Menu()
        {
            playerRoom = new Dictionary<long, int>();
            rooms = new Dictionary<int, GameRoom>();

            waitingForRoomId = false;

            var menu = new KeyboardButton[2][];
            menu[0] = new KeyboardButton[] {str_newroom};
            menu[1] = new KeyboardButton[] {str_joinroom};
            markupMenu = new ReplyKeyboardMarkup(menu, true);

            markupCancel = new ReplyKeyboardMarkup(
                new KeyboardButton[] {str_cancel}, true
            );

            markupHide = new ReplyKeyboardHide {HideKeyboard = true};
        }

        public static void HandleMessage(Message msg)
        {
            var chatId = msg.Chat.Id;

            if (playerRoom.ContainsKey(chatId))
            {
                rooms[playerRoom[chatId]].HandleMessage(msg);
            }
            else
            {
                if (!waitingForRoomId)
                    switch (msg.Text)
                    {
                        case str_newroom:
                        {
                            var room = new GameRoom(msg.Chat);
                            rooms.Add(room.ID, room);
                            playerRoom.Add(chatId, room.ID);
                            break;
                        }
                        case str_joinroom:
                        {
                            Program.Bot.SendTextMessageAsync(chatId, "Введите номер комнаты:",
                                false, false, 0, markupCancel
                            );
                            waitingForRoomId = true;
                            break;
                        }
                        default:
                        {
                            Program.Bot.SendTextMessageAsync(chatId,
                                "Добро пожаловать! Этот бот позволяет автоматически раздать роли в игре мафия." +
                                " Для начала игры создайте новую комнату или присоединитесь к существующей.",
                                false, false, 0, markupMenu);
                            break;
                        }
                    }
                else
                    switch (msg.Text)
                    {
                        case str_cancel:
                        {
                            Program.Bot.SendTextMessageAsync(chatId, "Возвращаемся в главное меню.", false, false, 0,
                                markupMenu);
                            waitingForRoomId = false;
                            break;
                        }
                        default:
                        {
                            try
                            {
                                var roomId = Convert.ToInt32(msg.Text);
                                rooms[roomId].EnterRoom(msg.Chat);
                                playerRoom.Add(chatId, roomId);
                                waitingForRoomId = false;
                            }
                            catch (FormatException)
                            {
                                Program.Bot.SendTextMessageAsync(chatId, "Это не похоже на число. Попробуйте ещё раз.",
                                    false, false, 0, markupCancel);
                            }
                            catch (KeyNotFoundException)
                            {
                                Program.Bot.SendTextMessageAsync(chatId, "Такой комнаты нет. Попробуйте другое число.",
                                    false, false, 0, markupCancel);
                            }
                            catch
                            {
                                Program.Bot.SendTextMessageAsync(chatId, "Пишёл нахув, сральник");
                            }

                            break;
                        }
                    }
            }
        }

        public static void LeaveRoom(long chatId)
        {
            playerRoom.Remove(chatId);
            Program.Bot.SendTextMessageAsync(chatId, "Вы покинули комнату.", false, false, 0, markupMenu);
        }
    }
}