namespace BeeWorld.Hooks;
public class BeePearlRead(DataPearl datapeal) : CosmeticSprite
{
    private int timer;
    private float start, end;
    readonly DataPearl datapearls = datapeal;

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        base.InitiateSprites(sLeaser, rCam);
        sLeaser.sprites = new FSprite[2];
        sLeaser.sprites[0] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["VectorCircle"],
            scale = 0,
            color = Custom.hexToColor("AEA7F1"),
        };
        sLeaser.sprites[1] = new FSprite("atlases/box", true)
        {
            scale = 0,
            color = Custom.hexToColor("AEA7F1"),
        };
        AddToContainer(sLeaser, rCam, null);
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        sLeaser.sprites[0].SetPosition(Vector2.Lerp(datapearls.firstChunk.lastPos, datapearls.firstChunk.pos, timeStacker) - camPos);
        sLeaser.sprites[1].SetPosition(Vector2.Lerp(datapearls.firstChunk.lastPos, datapearls.firstChunk.pos, timeStacker) - camPos);
        sLeaser.sprites[1].rotation -= 1;
        if (timer > 10 && timer <= 500)
        {
            if (start < 100) { start++; }
            float total = (start / 100f);
            float sineValue = Mathf.Sin(total * Mathf.PI * 0.5f); // Sine wave for smooth start
            sLeaser.sprites[1].scale = Mathf.Lerp(0, 0.04f, sineValue);
            sLeaser.sprites[0].scale = Mathf.Lerp(0, 1, sineValue);
            sLeaser.sprites[0].alpha = Mathf.Lerp(1, 0.09803921568f, sineValue);

        }
        if (timer > 500)
        {
            if (end < 100) { end++; }
            float total = (end / 100f);
            float sineValue = Mathf.Sin(total * Mathf.PI * 0.5f); // Sine wave for smooth end
            sLeaser.sprites[1].scale = Mathf.Lerp(0.04f, 0, sineValue);
            sLeaser.sprites[0].scale = Mathf.Lerp(1, 0, sineValue);
            sLeaser.sprites[0].alpha = Mathf.Lerp(0.09803921568f, 1, sineValue);

        }
    }
    public override void Update(bool eu)
    {
        base.Update(eu);
        timer++;
        if (end >= 255)
        {
            Destroy();
        }
    }
}