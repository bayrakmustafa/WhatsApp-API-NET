/**
 * Copyright (C) 2015 langboost
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

namespace Tr.Com.Eimza.LibAxolotl.Ecc.Impl
{
    /// <summary>
    /// If you want to expose an implementation of Curve25519 to this class library,
    /// implement this interface.
    /// </summary>
    public interface ICurve25519Provider
    {
        byte[] CalculateAgreement(byte[] ourPrivate, byte[] theirPublic);

        byte[] CalculateSignature(byte[] random, byte[] privateKey, byte[] message);

        byte[] GeneratePrivateKey(byte[] random);

        byte[] GeneratePublicKey(byte[] privateKey);

        bool IsNative();

        bool VerifySignature(byte[] publicKey, byte[] message, byte[] signature);
    }
}