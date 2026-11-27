using System;
using System.Collections.Generic;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace DivineRoot.Content.Projectiles
{
    public static class SistersTailShaderDrawer
    {
        public static float GetSwingProgress(Projectile projectile)
        {
            Projectile.GetWhipSettings(projectile, out float timeToFlyOut, out int _, out float _);
            return Utils.GetLerpValue(0f, timeToFlyOut, projectile.ai[0], clamped: true);
        }

        public static void RecordTipHistory(
            Projectile projectile,
            List<Vector2> controlPoints,
            List<Vector2> tipHistory,
            int maxPoints = 24)
        {
            if (controlPoints.Count == 0)
                return;

            float swingProgress = GetSwingProgress(projectile);

            if (swingProgress > 0.08f && swingProgress < 0.92f)
            {
                Vector2 tip = controlPoints[^1];
                if (tipHistory.Count == 0 || Vector2.DistanceSquared(tipHistory[^1], tip) > 4f)
                    tipHistory.Add(tip);

                if (tipHistory.Count > maxPoints)
                    tipHistory.RemoveAt(0);
            }
            else if (tipHistory.Count > 0)
            {
                tipHistory.RemoveAt(0);
            }
        }

        public static void RenderWhipPrimitives(
            Projectile projectile,
            List<Vector2> tipHistory,
            bool empowered)
        {
            List<Vector2> controlPoints = new List<Vector2>();
            Projectile.FillWhipControlPoints(projectile, controlPoints);

            float swingProgress = GetSwingProgress(projectile);
            float swingEnvelope = Utils.GetLerpValue(0f, 0.15f, swingProgress, true) *
                                  Utils.GetLerpValue(1f, 0.78f, swingProgress, true);

            if (swingEnvelope <= 0.01f)
                return;

            ManagedShader shader = ShaderManager.GetShader("DivineRoot.SistersTailShader");
            shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
            shader.TrySetParameter("uTime", Main.GlobalTimeWrappedHourly);

            if (controlPoints.Count >= 2)
            {
                float cordWidth = empowered ? 22f : 13f;
                Color primaryCord = empowered ? new Color(220, 20, 45) : new Color(165, 10, 30);
                Color secondaryCord = empowered ? new Color(255, 190, 190) : new Color(255, 75, 90);

                shader.TrySetParameter("uIntensity", empowered ? 2.1f : 1.35f);
                shader.TrySetParameter("uSecondaryColor", secondaryCord.ToVector3());

                PrimitiveSettings cordSettings = new PrimitiveSettings(
                    completion => MathF.Sin(completion * MathHelper.Pi) * cordWidth * swingEnvelope + 4f,
                    completion => Color.Lerp(new Color(90, 0, 20), primaryCord, completion) * swingEnvelope,
                    Smoothen: true,
                    Pixelate: true,
                    Shader: shader
                );

                PrimitiveRenderer.RenderTrail(controlPoints, cordSettings, 36);
            }

            if (tipHistory.Count >= 2)
            {
                float slashWidth = empowered ? 36f : 20f;
                Color primarySlash = empowered ? new Color(245, 30, 60) : new Color(190, 15, 35);
                Color secondarySlash = empowered ? new Color(255, 225, 220) : new Color(255, 110, 110);

                shader.TrySetParameter("uIntensity", empowered ? 2.6f : 1.65f);
                shader.TrySetParameter("uSecondaryColor", secondarySlash.ToVector3());

                PrimitiveSettings slashSettings = new PrimitiveSettings(
                    completion => MathHelper.Lerp(2f, slashWidth, completion) * swingEnvelope,
                    completion => primarySlash * completion * swingEnvelope,
                    Smoothen: true,
                    Pixelate: true,
                    Shader: shader
                );

                PrimitiveRenderer.RenderTrail(tipHistory, slashSettings, 32);
            }
        }
    }
}