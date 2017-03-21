public interface IGameConfig
{
    double GetTimeStep();
    double GetMapX();
    double GetMapY();
    double GetMapWidth();
    double GetMapHeight();
    double GetMaxRadius();
    double GetMapBoundFix();
    int GetMoveTimes();
    int GetCommandDelay();
    int GetSpawnStep();
    int GetMoneyPerStep();
    int GetDefaultMoney();
}
