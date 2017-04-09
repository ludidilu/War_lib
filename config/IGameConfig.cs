public interface IGameConfig
{
    double GetTimeStep();
    double GetMapWidth();
    double GetMapHeight();
    double GetSpawnX();
    double GetSpawnY();
    double GetBaseX();
    double GetBaseY();
    int GetBaseID();
    double GetMaxRadius();
    double GetMapBoundFix();
    int GetMoveTimes();
    int GetCommandDelay();
    int GetSpawnStep();
    int GetMoneyPerStep();
    int GetDefaultMoney();
}
