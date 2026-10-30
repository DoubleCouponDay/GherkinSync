using GherkinSync;

namespace TestProject
{
    [SyncedFeature("Valid Login.feature")]
    [SyncedTest("Valid Login.feature")]
    public class ValidLogin {
        public void GivenIAmOnTheLoginPage()
        {
        }

        public void WhenIEnterValidCredentials()
        {

        }

        public void ThenIShouldBeLoggedIn() { }

        public void GivenICantLogInYet() { }

        public void WhenIRetryWithTheRightPassword() { }

        public void ThenImLoggedIn() { }
    }
}
