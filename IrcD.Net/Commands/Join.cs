﻿/*
 *  The ircd.net project is an IRC deamon implementation for the .NET Plattform
 *  It should run on both .NET and Mono
 * 
 *  Copyright (c) 2009-2010 Thomas Bruderer <apophis@apophis.ch>
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrcD.ServerReplies;

namespace IrcD.Commands
{
    public class Join : CommandBase
    {
        public Join(IrcDaemon ircDaemon)
            : base(ircDaemon, "JOIN")
        { }

        public override void Handle(UserInfo info, List<string> args)
        {
            if (!info.Registered)
            {
                IrcDaemon.Replies.SendNotRegistered(info);
                return;
            }
            if (args.Count < 1)
            {
                IrcDaemon.Replies.SendNeedMoreParams(info);
                return;
            }
            if (args[0] == "0")
            {
                PartAll(info);
                return;
            }

            foreach (var channel in from temp in GetSubArgument(args[0])
                                    where !info.UserPerChannelInfos.Any(upci => upci.ChannelInfo.Name == temp)
                                    select temp)
            {
                ChannelInfo chan;

                if (IrcDaemon.Channels.ContainsKey(channel))
                {
                    chan = IrcDaemon.Channels[channel];

                    if (!chan.Modes.HandleEvent(IrcCommandType.Join, chan, info, args))
                    {
                        continue;
                    }
                }
                else
                {
                    chan = new ChannelInfo(channel, IrcDaemon);
                    IrcDaemon.Channels.Add(chan.Name, chan);
                }

                var chanuser = new UserPerChannelInfo(info, chan);
                chan.UserPerChannelInfos.Add(info.Nick, chanuser);
                info.UserPerChannelInfos.Add(chanuser);

                IrcDaemon.Send.Join(info, chan, chan);

                SendTopic(info, chan);
                IrcDaemon.Replies.SendNamesReply(chanuser.UserInfo, chan);
                IrcDaemon.Replies.SendEndOfNamesReply(info, chan);
            }
        }

        private void PartAll(UserInfo info)
        {
            var command = new StringBuilder();
            var partargs = new List<string>();
            // this is a part all channels, this is plainly stupid to handle PARTS in a join message.
            // we won't handle that, we give it to the part handler! YO! why not defining a /part * instead of /join 0

            command.Length = 0; bool first = true;
            foreach (var ci in info.Channels)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    command.Append(",");
                }
                command.Append(ci.Name);
            }
            partargs.Add(command.ToString());
            partargs.Add(IrcDaemon.Options.StandardPartMessage);
            IrcDaemon.Commands.Handle("PART", info, partargs);
        }

        private void SendTopic(UserInfo info, ChannelInfo chan)
        {
            if (string.IsNullOrEmpty(chan.Topic))
            {
                IrcDaemon.Replies.SendNoTopicReply(info, chan);
            }
            else
            {
                IrcDaemon.Replies.SendTopicReply(info, chan);
            }
        }
    }
}
