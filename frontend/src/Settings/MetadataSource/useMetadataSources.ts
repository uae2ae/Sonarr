import { useMemo } from 'react';
import { useManageProviderSettings, useProviderSettings } from 'Settings/useProviderSettings';
import Provider from 'typings/Provider';
import { sortByProp } from 'Utilities/Array/sortByProp';

export interface MetadataSourceModel extends Provider {
  enable: boolean;
  priority: number;
  tags: number[];
}

const PATH = '/metadatasource';

export const useMetadataSourceItem = (id: number | undefined) => {
  const { data } = useMetadataSources();

  if (id === undefined) {
    return undefined;
  }

  return data.find((source) => source.id === id);
};

export const useMetadataSources = () => {
  return useProviderSettings<MetadataSourceModel>({
    path: PATH,
  });
};

export const useSortedMetadataSources = () => {
  const result = useMetadataSources();

  const sortedData = useMemo(
    () => [...result.data].sort(sortByProp('priority')),
    [result.data]
  );

  return {
    ...result,
    data: sortedData,
  };
};

export const useManageMetadataSource = (id: number | undefined) => {
  const source = useMetadataSourceItem(id);

  const manage = useManageProviderSettings<MetadataSourceModel>(
    id,
    source ?? ({} as MetadataSourceModel),
    PATH
  );

  return manage;
};
