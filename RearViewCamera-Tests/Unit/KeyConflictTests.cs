using System.Collections.Generic;
using RearViewCamera.Logic;

namespace RearViewCamera.Tests.Unit
{
    /// <summary>D-035 / D-038: configured key, else Home / End, else off.</summary>
    [TestFixture]
    public class KeyConflictTests
    {
        const int N = 110, Home = 278, J = 106;

        static KeyState Run(int configured, int backup, HashSet<int> taken, out int used)
        {
            return KeyConflict.Resolve(configured, backup, k => taken.Contains(k), out used);
        }

        [Test]
        public void Free_UsesConfigured()
        {
            Assert.That(Run(N, Home, new HashSet<int> { J }, out int used), Is.EqualTo(KeyState.Free));
            Assert.That(used, Is.EqualTo(N));
        }

        [Test]
        public void Taken_UsesBackup()
        {
            Assert.That(Run(N, Home, new HashSet<int> { N }, out int used), Is.EqualTo(KeyState.Backup));
            Assert.That(used, Is.EqualTo(Home));
        }

        [Test]
        public void BackupTakenToo_Off()
        {
            Assert.That(Run(N, Home, new HashSet<int> { N, Home }, out int used), Is.EqualTo(KeyState.Off));
            Assert.That(used, Is.EqualTo(0));
        }

        [Test]
        public void None_Unbound()
        {
            Assert.That(Run(0, Home, new HashSet<int>(), out int used), Is.EqualTo(KeyState.Unbound));
            Assert.That(used, Is.EqualTo(0));
        }
    }
}
