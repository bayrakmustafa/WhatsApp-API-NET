/**
 * Copyright (C) 2015 smndtrl
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using static Tr.Com.Eimza.LibAxolotl.State.StorageProtos;

namespace Tr.Com.Eimza.LibAxolotl.State
{
    /**
 * A SessionRecord encapsulates the state of an ongoing session.
 *
 * @author Moxie Marlinspike
 */

    public class SessionRecord
    {
        private static int ARCHIVED_STATES_MAX_LENGTH = 40;

        private SessionState sessionState = new SessionState();
        private LinkedList<SessionState> previousStates = new LinkedList<SessionState>();
        private bool fresh = false;

        public SessionRecord()
        {
            this.fresh = true;
        }

        public SessionRecord(SessionState sessionState)
        {
            this.sessionState = sessionState;
            this.fresh = false;
        }

        public SessionRecord(byte[] serialized)
        {
            RecordStructure record = RecordStructure.ParseFrom(serialized);
            this.sessionState = new SessionState(record.CurrentSession);
            this.fresh = false;

            foreach (SessionStructure previousStructure in record.PreviousSessionsList)
            {
                previousStates.AddLast(new SessionState(previousStructure)); // add -> AddLast (java)
            }
        }

        public bool HasSessionState(uint version, byte[] aliceBaseKey)
        {
            if (sessionState.GetSessionVersion() == version &&
                Enumerable.SequenceEqual(aliceBaseKey, sessionState.GetAliceBaseKey()))
            {
                return true;
            }

            foreach (SessionState state in previousStates)
            {
                if (state.GetSessionVersion() == version &&
                    Enumerable.SequenceEqual(aliceBaseKey, state.GetAliceBaseKey()))
                {
                    return true;
                }
            }

            return false;
        }

        public SessionState GetSessionState()
        {
            return sessionState;
        }

        /**
         * @return the list of all currently maintained "previous" session states.
         */

        public LinkedList<SessionState> GetPreviousSessionStates()
        {
            return previousStates;
        }

        public bool IsFresh()
        {
            return fresh;
        }

        /**
         * Move the current {@link SessionState} into the list of "previous" session states,
         * and replace the current {@link org.whispersystems.libaxolotl.state.SessionState}
         * with a fresh reset instance.
         */

        public void ArchiveCurrentState()
        {
            PromoteState(new SessionState());
        }

        public void PromoteState(SessionState promotedState)
        {
            this.previousStates.AddFirst(sessionState);
            this.sessionState = promotedState;

            if (previousStates.Count > ARCHIVED_STATES_MAX_LENGTH)
            {
                previousStates.RemoveLast();
            }
        }

        public void SetState(SessionState sessionState)
        {
            this.sessionState = sessionState;
        }

        /**
         * @return a serialized version of the current SessionRecord.
         */

        public byte[] Serialize()
        {
            List<SessionStructure> previousStructures = new List<SessionStructure>();

            foreach (SessionState previousState in previousStates)
            {
                previousStructures.Add(previousState.GetStructure());
            }

            RecordStructure record = RecordStructure.CreateBuilder()
                                                    .SetCurrentSession(sessionState.GetStructure())
                                                    .AddRangePreviousSessions(previousStructures)
                                                    /*.AddAllPreviousSessions(previousStructures)*/
                                                    .Build();

            return record.ToByteArray();
        }
    }
}