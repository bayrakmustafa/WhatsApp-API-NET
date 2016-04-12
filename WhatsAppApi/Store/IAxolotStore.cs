using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tr.Com.Eimza.LibAxolotl.Groups.State;
using Tr.Com.Eimza.LibAxolotl.State;

namespace WhatsAppApi.Store
{
    public interface IAxolotStore : PreKeyStore, SignedPreKeyStore, IdentityKeyStore, SessionStore, SenderKeyStore
    {
        void Clear();

        void ClearRecipient(String recipientId);
    }
}
