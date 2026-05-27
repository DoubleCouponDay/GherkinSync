use gherkin_sync::synced_feature;

struct MyFeature;

#[synced_feature("Valid_Login.feature")]
impl MyFeature {
    fn given_i_am_on_the_login_page(&self) {

    }

    fn when_i_enter_valid_credentials(&self) {

    }

    fn then_i_should_be_logged_in(&self) {

    }
}
