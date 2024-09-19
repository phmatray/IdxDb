// import {
//   openIndexedDB,
//   addOne,
//   getAll,
//   getOne,
//   updateOne,
//   deleteOne,
//   upgradeDatabase,
//   addMany,
//   createIndex,
//   getAllByIndex,
//   beginTransaction,
//   commitTransaction,
//   count,
//   clearStore,
// } from "../../IndexedDb/wwwroot/idb";

import {jest} from '@jest/globals';
import {indexedDB, IDBKeyRange} from "fake-indexeddb";
import {sum, openIndexedDB} from '../../IndexedDb/wwwroot/idb';

jest.useFakeTimers();

test('adds 1 + 2 to equal 3', () => {
  expect(sum(1, 2)).toBe(3);
});

test("openIndexedDB should open and cache the database", async () => {
  const dbName = "TestDB";
  const dbVersion = 1;

  const db = await openIndexedDB(dbName, dbVersion);

  expect(db).toBeDefined();
  expect(db.name).toBe(dbName);
  expect(db.version).toBe(dbVersion);

  // Attempt to open again and ensure it's cached
  const cachedDb = await openIndexedDB(dbName);
  expect(cachedDb).toBe(db);
});