using System.Linq;

namespace BreadLibrary.Core.Graphics.Metaballs
{
    public sealed class MetaballRenderer : ModSystem
    {
        private static readonly BlendState AdditiveFieldBlend = new()
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,

            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add,

            ColorWriteChannels = ColorWriteChannels.All
        };

        public static float DesiredScale => 1f;
        private static Asset<Effect> metaballEffect;
        
        private static Asset<Effect> metaballResolveEffect;

        public static RenderTarget2D metaballTarget;
        public static RenderTarget2D postProcessTarget;
        //another one!
        //So that multiple metaballs can co-exist in peace and harmony!!
        private static RenderTarget2D layerCompositeTarget;

        private static Texture2D blankTexture;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            metaballEffect = ModContent.Request<Effect>(
                "BreadLibrary/Assets/Effects/Metaballs/MetaballField",
                AssetRequestMode.ImmediateLoad
            );
            metaballResolveEffect = ModContent.Request<Effect>(
            "BreadLibrary/Assets/Effects/Metaballs/MetaballResolve",
            AssetRequestMode.ImmediateLoad);
            On_Main.CheckMonoliths += RenderMetaballs;
        }   
        private static void BuildLayerTarget(MetaballLayer layer)   
        {
            if (Main.dedServ || metaballEffect?.Value is null || metaballResolveEffect?.Value is null)
                return;

            if (!MetaballSystem.AnythingToDraw)
                return;

            EnsureTargets();

            GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();

            graphicsDevice.SetRenderTarget(layerCompositeTarget);
            graphicsDevice.Clear(Color.Transparent);

            foreach (IGrouping<Metaball, MetaballGroup> groupSet in MetaballSystem.GetGroupSets(layer))
                DrawGroupSetToComposite(graphicsDevice, spriteBatch, groupSet, layerCompositeTarget);


            graphicsDevice.SetRenderTargets(previousTargets);
        }
        private void RenderMetaballs(On_Main.orig_CheckMonoliths orig)
        {
            orig();
            BuildLayerTarget(MetaballLayer.AboveTiles);
        }

        public override void Unload()
        {
            metaballEffect = null;
            metaballResolveEffect = null;

            metaballTarget?.Dispose();
            metaballTarget = null;

            postProcessTarget?.Dispose();
            postProcessTarget = null;

            blankTexture = null;

            layerCompositeTarget?.Dispose();
            layerCompositeTarget = null;

            On_Main.CheckMonoliths -= RenderMetaballs;
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ)
                return;

            //Don't render if nothing to draw, because who would thunk that metaballs be expensive?
            if (!MetaballSystem.AnythingToDraw)
                return;

            Matrix scale = Main.GameViewMatrix.TransformationMatrix;
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                scale
            );

            Main.spriteBatch.Draw(layerCompositeTarget, Vector2.Zero, Color.White);

            Main.spriteBatch.End();
        }

    

        private static void DrawGroupSetToComposite(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch,
    IGrouping<Metaball, MetaballGroup> groupSet, RenderTarget2D compositeTarget)
        {
            Metaball metaball = groupSet.Key;

            Effect fieldEffect = metaballEffect.Value;
            Effect resolveEffect = metaballResolveEffect.Value;
            Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);

            Rectangle drawBounds = CalculateGroupSetScreenBounds(
                groupSet,
                Main.screenPosition,
                screenSize,
                24f
            );

            if (drawBounds.IsEmpty)
                return;

            fieldEffect.Parameters["uScreenSize"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            fieldEffect.Parameters["uDrawBounds"]?.SetValue(new Vector4(
                drawBounds.X,
                drawBounds.Y,
                drawBounds.Width,
                drawBounds.Height
            ));

          
            MetaballDrawContext fieldContext = new(
                graphicsDevice,
                metaballTarget,
                Main.screenPosition,
                new Vector2(Main.screenWidth, Main.screenHeight),
                Main.GlobalTimeWrappedHourly
            );

            metaball.ApplyShaderParameters(fieldEffect, fieldContext);

            graphicsDevice.SetRenderTarget(metaballTarget);
            graphicsDevice.Clear(Color.Transparent);

            int totalRenderedInstances = 0;

            foreach (MetaballGroup group in groupSet)
            {
                int instanceCount = group.FillShaderBufferVisible(fieldContext.ScreenPosition, fieldContext.ScreenSize);

                if (instanceCount <= 0)
                    continue;


                totalRenderedInstances += instanceCount;

                fieldEffect.Parameters["uMetaballs"]?.SetValue(group.ShaderBuffer);
                fieldEffect.Parameters["uMetaballCount"]?.SetValue(instanceCount);

                spriteBatch.Begin(
                    SpriteSortMode.Immediate,
                    AdditiveFieldBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    fieldEffect,
                    Matrix.Identity
                );

                spriteBatch.Draw(GetBlankTexture(), drawBounds, Color.White);

                spriteBatch.End();
            }

            if (totalRenderedInstances <= 0)
                return;

            graphicsDevice.SetRenderTarget(postProcessTarget);
            graphicsDevice.Clear(Color.Transparent);

            resolveEffect.Parameters["uImage0"]?.SetValue(metaballTarget);
            resolveEffect.Parameters["uColor"]?.SetValue(metaball.Color.ToVector4());
            resolveEffect.Parameters["uThreshold"]?.SetValue(metaball.Threshold);
            resolveEffect.Parameters["uEdgeSoftness"]?.SetValue(metaball.EdgeSoftness);

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                resolveEffect,
                Matrix.Identity
            );

            spriteBatch.Draw(metaballTarget, Vector2.Zero, Color.White);

            spriteBatch.End();

            graphicsDevice.SetRenderTarget(compositeTarget);

            Effect postProcessEffect = metaball.PostProcessEffect;

            if (postProcessEffect is not null)
            {
                MetaballDrawContext postContext = new(
                    graphicsDevice,
                    postProcessTarget,
                    Main.screenPosition,
                    new Vector2(Main.screenWidth, Main.screenHeight),
                    Main.GlobalTimeWrappedHourly
                );
                postProcessEffect.Parameters["uImage0"]?.SetValue(postProcessTarget);
                postProcessEffect.Parameters["uRawField"]?.SetValue(metaballTarget);

                postProcessEffect.Parameters["uScreenSize"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
                postProcessEffect.Parameters["uColor"]?.SetValue(metaball.Color.ToVector4());
                postProcessEffect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly * 0.05f); 

                metaball.ApplyPostProcessParameters(postProcessEffect, postContext);

                spriteBatch.Begin(
                     SpriteSortMode.Immediate,
                     BlendState.AlphaBlend,
                     SamplerState.LinearClamp,
                     DepthStencilState.None,
                     RasterizerState.CullNone,
                     postProcessEffect,
                     Matrix.Identity
                 );

                spriteBatch.Draw(postProcessTarget, Vector2.Zero, Color.White);

                spriteBatch.End();
            }
            else
            {
                spriteBatch.Begin(
                  SpriteSortMode.Immediate,
                  BlendState.AlphaBlend,
                  SamplerState.PointClamp,
                  DepthStencilState.None,
                  RasterizerState.CullNone,
                  null,
                  Matrix.Identity
              );

                spriteBatch.Draw(postProcessTarget, Vector2.Zero, Color.White);

                spriteBatch.End();
            }
        }
        private static Rectangle CalculateGroupSetScreenBounds(
            IGrouping<Metaball, MetaballGroup> groupSet,
            Vector2 screenPosition,
            Vector2 screenSize,
            float padding = 16f)
        {
            float minX = screenSize.X;
            float minY = screenSize.Y;
            float maxX = 0f;
            float maxY = 0f;

            bool foundAny = false;

            foreach (MetaballGroup group in groupSet)
            {
                foreach (MetaballInstance instance in group.Instances)
                {
                    if (!instance.IntersectsScreen(screenPosition, screenSize, padding))
                        continue;

                    Vector2 center = instance.Center - screenPosition;
                    float radius = instance.Radius + padding;

                    minX = MathF.Min(minX, center.X - radius);
                    minY = MathF.Min(minY, center.Y - radius);
                    maxX = MathF.Max(maxX, center.X + radius);
                    maxY = MathF.Max(maxY, center.Y + radius);

                    foundAny = true;
                }
            }

            if (!foundAny)
                return Rectangle.Empty;

            int x = Math.Max(0, (int)MathF.Floor(minX));
            int y = Math.Max(0, (int)MathF.Floor(minY));
            int right = Math.Min((int)screenSize.X, (int)MathF.Ceiling(maxX));
            int bottom = Math.Min((int)screenSize.Y, (int)MathF.Ceiling(maxY));

            int width = right - x;
            int height = bottom - y;

            if (width <= 0 || height <= 0)
                return Rectangle.Empty;

            return new Rectangle(x, y, width, height);
        }


        private static void EnsureTargets()
        {
            GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;

            

            bool needsTarget =
                metaballTarget is null ||
                metaballTarget.Width != (int)(Main.screenWidth/DesiredScale) ||
                metaballTarget.Height !=(int)( Main.screenHeight/DesiredScale) ||
                metaballTarget.IsDisposed;

            if (needsTarget)
            {
                metaballTarget?.Dispose();
                postProcessTarget?.Dispose();
                layerCompositeTarget?.Dispose();

                metaballTarget = new RenderTarget2D(
                    graphicsDevice,
                    (int)(Main.screenWidth/DesiredScale),
                    (int)(Main.screenHeight/DesiredScale),
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None,
                    0,
                    RenderTargetUsage.PreserveContents
                );

                postProcessTarget = new RenderTarget2D(
                    graphicsDevice,
                    (int)(Main.screenWidth / DesiredScale),
                    (int)(Main.screenHeight / DesiredScale),
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None,
                    0,
                    RenderTargetUsage.PreserveContents
                );

                layerCompositeTarget = new RenderTarget2D(
                    graphicsDevice,
                    (int)(Main.screenWidth / DesiredScale),
                    (int)(Main.screenHeight / DesiredScale),
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None,
                    0,
                    RenderTargetUsage.PreserveContents
                );
            }
        }

        private static Texture2D GetBlankTexture()
        {
            return TextureAssets.MagicPixel.Value;


            if (blankTexture is not null && !blankTexture.IsDisposed)
                return blankTexture;

            blankTexture = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
            blankTexture.SetData(new[] { Color.White });
            return blankTexture;
        }
    }
}