using GherkinSync;

namespace TestProject
{
    [SyncedFeature("Valid.feature")]
    public class ValidLogin
    {
        public void GivenIAmOnTheLogin() { }

        public void WhenIEnterValidCredentials() { }

        public void ThenIShouldBeLoggedIn() { }
    }
}
