﻿namespace HueDream.DreamScreen.Scenes {
    public class SceneTwinkle : SceneBase {
        public SceneTwinkle() {
            SetColors(new string[] {
                "AAAAAA",
                "000000",
                "AAAAAA"
            });
            AnimationTime = 1;
            Mode = AnimationMode.Random;
            Easing = EasingType.blend;
        }
    }
}