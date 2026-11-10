using System.Collections.Generic;

namespace Bms88122
{
    public class BmsServices : SqlHelper
    {
        public int Insert(BmsDataModel bmsDataModel)
        {
            bmsDataModel.CreateTime = DbSugarContext.GetDate();
            return DbSugarContext.Insertable(bmsDataModel).ExecuteCommand();
        }

        public bool PackCodeExists(string packCode)
        {
            if (string.IsNullOrWhiteSpace(packCode))
                return false;
            return DbSugarContext.Queryable<BmsDataModel>().Where(o => o.PackCode == packCode).Any();
        }

        public List<PQCSettingModel> GetPQCSettingModels()
        {
            return DbSugarContext.Queryable<PQCSettingModel>().ToList();
        }
    }
}
