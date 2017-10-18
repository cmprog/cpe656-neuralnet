internal sealed class IdGenerator
{
    private int mNextId;

    public IdGenerator()
    {
        this.mNextId = 0;
    }

    public int Next()
    {
        return this.mNextId++;
    }
}