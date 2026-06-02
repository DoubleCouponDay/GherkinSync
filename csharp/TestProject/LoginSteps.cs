using GherkinSync;

namespace TestProject
{
    [SyncedTest("Valid Login.feature")]
    public class ValidLogin
    {
        public void GivenIAmOnTheLoginPage() { }

        public void WhenIEnterValidCredentials() { }

        public void ThenIShouldBeLoggedIn() { }
    }
}
