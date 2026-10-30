
use gherkin_sync::{synced_feature, synced_test};

#[derive(Default)]
struct ValidLogin;

#[allow(dead_code)]
#[synced_feature("Valid_Login.feature")]
#[synced_test("Valid_Login.feature")]
impl ValidLogin {
    fn given_i_am_on_the_login_page(&self) {
        // arrange: navigate to login page
    }

    fn when_i_enter_valid_credentials(&self) {
        // act: submit valid credentials
    }

    fn then_i_should_be_logged_in(&self) {
        // assert: user is authenticated
    }

    fn given_i_cant_log_in_yet(&self) {}

    fn when_i_retry_with_the_right_password(&self) {}

    fn then_im_logged_in(&self) {}
}
