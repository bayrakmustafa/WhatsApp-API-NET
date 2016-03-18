using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tr.Com.Eimza.LibAxolotl
{
    public interface DecryptionCallback
    {
        void HandlePlaintext(byte[] plaintext);

    }
}
