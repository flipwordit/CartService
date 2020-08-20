declare @date datetime = getdate()
declare @date10 datetime=dateadd(day, -10, @date)
declare @date20 datetime=dateadd(day, -20, @date)
declare @date30 datetime=dateadd(day, -30, @date)
                                                                                                                                                                                                                                                                                                            

select count(1) as [count], 
sum(case when forBonusPoints>0 then 1 else 0 end) [countWithForBonusProducts],
sum(case when Edited between @date30 and @date20  then 1 else 0 end) [expired10],
sum(case when Edited between @date20 and @date10  then 1 else 0 end) [expired20],
sum(case when Edited between @date20 and @date  then 1 else 0 end) [expired30]
from (
SELECT sc.id, sc.Edited,max(case when p.ForBonusPoints=1 then 1 else 0 end) as forBonusPoints, sum(p.Cost*scp.Amount) cartSum

  FROM [dbo].[ShoppingCart] sc
  join ShoppingCartProducts scp
  on sc.id=scp.ShoppingCart_id
  join Product p on scp.Product_id=p.id
  group by sc.Id, sc.Edited) t
  